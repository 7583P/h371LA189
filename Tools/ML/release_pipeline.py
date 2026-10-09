"""Local candidate training/evaluation only. Never installs a runtime model."""
import argparse
import hashlib
import json
import time
import struct
from pathlib import Path
import numpy as np
import torch
from torch import nn
from torch.utils.data import DataLoader, TensorDataset

from racing_dataset import (FEATURE_NAMES, FEATURE_VERSION, FEATURE_GROUPS, OUTPUT_NAMES,
                           load_dataset, split_sessions, make_windows, assign_training_pace_reference)
from train_behavior import BehaviorCloningMLP
from train_temporal import TemporalDrivingGRU


class CompactAttentionGRU(nn.Module):
    def __init__(self, input_dim, output_dim=8):
        super().__init__()
        self.gru = nn.GRU(input_dim, 32, batch_first=True)
        self.attention = nn.Linear(32, 1)
        self.head = nn.Sequential(nn.Linear(64, 32), nn.ReLU(), nn.Linear(32, output_dim))

    def forward(self, x):
        history, _ = self.gru(x)
        weights = torch.softmax(self.attention(history), dim=1)
        context = torch.sum(history * weights, dim=1)
        raw = self.head(torch.cat((history[:, -1], context), dim=-1))
        return torch.cat((torch.tanh(raw[:, :1]), torch.sigmoid(raw[:, 1:6]), torch.tanh(raw[:, 6:])), dim=-1)


class ContextMLP(BehaviorCloningMLP):
    def forward(self, x):
        raw = self.net(x[:, -1])
        return torch.cat((torch.tanh(raw[:, :1]), torch.sigmoid(raw[:, 1:6]), torch.tanh(raw[:, 6:])), dim=-1)


class ContextGRU(TemporalDrivingGRU):
    def forward(self, x):
        history, _ = self.gru(x)
        raw = self.fc(history[:, -1])
        return torch.cat((torch.tanh(raw[:, :1]), torch.sigmoid(raw[:, 1:6]), torch.tanh(raw[:, 6:])), dim=-1)


def metrics(pred, target):
    result = {"continuous_mae": {OUTPUT_NAMES[i]: float(np.mean(np.abs(pred[:, i]-target[:, i]))) for i in (0,1,2,6,7)}}
    result["discrete"] = {}
    for i in (3,4,5):
        actual, proposed = target[:, i]>.5, pred[:, i]>.5
        tp, fp, fn = int(np.sum(actual & proposed)), int(np.sum(~actual & proposed)), int(np.sum(actual & ~proposed))
        support = int(actual.sum())
        precision, recall = tp/max(1,tp+fp), tp/max(1,tp+fn)
        result["discrete"][OUTPUT_NAMES[i]] = dict(positives=support, false_positives=fp,
            precision=precision if support else None, recall=recall if support else None,
            f1=2*precision*recall/max(1e-9,precision+recall) if support else None)
    return result


def predict(model, features, device):
    outputs=[]
    with torch.no_grad():
        for start in range(0,len(features),512):
            outputs.append(model(torch.from_numpy(features[start:start+512]).to(device)).cpu().numpy())
    return np.concatenate(outputs)


def train_candidate(kind, train, validation, output, device, epochs, seed):
    torch.manual_seed(seed)
    factories = {"behavior_mlp": ContextMLP, "temporal_gru": ContextGRU, "compact_attention_gru": CompactAttentionGRU}
    model=factories[kind](input_dim=train[0].shape[-1],output_dim=8).to(device)
    loader=DataLoader(TensorDataset(*[torch.from_numpy(x) for x in train]),batch_size=256,shuffle=True)
    optimizer=torch.optim.AdamW(model.parameters(),lr=.001,weight_decay=.0001)
    counts=train[1][:,3:6].sum(axis=0)
    positive_weights=torch.tensor(np.clip(np.sqrt((len(train[1])-counts)/np.maximum(1,counts)),1,8),device=device)
    best=float('inf'); history=[]
    def loss(pred,y,w):
        continuous=((pred[:,[0,1,2,6,7]]-y[:,[0,1,2,6,7]])**2).mean(dim=1)
        p=pred[:,3:6].clamp(1e-6,1-1e-6); truth=y[:,3:6]
        discrete=-(truth*torch.log(p)*positive_weights+(1-truth)*torch.log(1-p)).mean(dim=1)
        return ((continuous+discrete*.25)*w).sum()/w.sum().clamp_min(1e-6)
    path=output/(kind+'.pt')
    for epoch in range(epochs):
        model.train(); total=0
        for x,y,w in loader:
            x,y,w=x.to(device),y.to(device),w.to(device)
            optimizer.zero_grad(); value=loss(model(x),y,w); value.backward()
            nn.utils.clip_grad_norm_(model.parameters(),1); optimizer.step(); total+=float(value.detach())
        model.eval()
        with torch.no_grad():
            x,y,w=[torch.from_numpy(a).to(device) for a in validation]
            val=float(loss(model(x),y,w))
        history.append(dict(epoch=epoch+1,train_loss=total/max(1,len(loader)),validation_loss=val))
        if val<best:
            best=val
            torch.save(dict(model_state_dict=model.state_dict(),model_type=kind,feature_version=FEATURE_VERSION,
                input_dim=len(FEATURE_NAMES),output_dim=8,sequence_length=train[0].shape[1],epoch=epoch+1,
                target_alignment='NEXT_TELEMETRY_COMMAND'),path)
        print(kind,epoch+1,round(val,6),flush=True)
    model.load_state_dict(torch.load(path,map_location=device,weights_only=True)['model_state_dict']); model.eval()
    return model,history


def parity_indices(example,targets,count):
    """Cover every MMR and rare action, then span the complete held-out history."""
    count=min(count,len(example)); selected=[]
    def add(indices,limit):
        if not len(indices):return
        for i in indices[np.linspace(0,len(indices)-1,min(limit,len(indices)),dtype=int)]:
            value=int(i)
            if value not in selected and len(selected)<count:selected.append(value)
    for rating in np.unique(example[:,-1,0]):add(np.flatnonzero(example[:,-1,0]==rating),1)
    if targets is not None:
        for output in (3,4,5):add(np.flatnonzero(targets[:,output]>.5),8)
        for output in (0,6,7):add(np.flatnonzero(np.abs(targets[:,output])>.2),4)
    add(np.flatnonzero(example[:,-1,8]>.5),8)
    add(np.arange(len(example)),count)
    if len(selected)<count:add(np.arange(len(example)),len(example))
    return np.asarray(sorted(selected),dtype=int)


def evaluate_runtime(model, example, path, targets=None):
    import onnx
    import onnxruntime as ort
    cpu_model=model.to('cpu').eval()
    tensor=torch.from_numpy(example[:1])
    torch.onnx.export(cpu_model,tensor,str(path),input_names=['history'],output_names=['actions'],opset_version=17,
                      dynamic_axes={'history':{0:'batch'},'actions':{0:'batch'}},dynamo=False)
    onnx.checker.check_model(onnx.load(str(path)))
    options=ort.SessionOptions(); options.intra_op_num_threads=1
    session=ort.InferenceSession(str(path),sess_options=options,providers=['CPUExecutionProvider'])
    indices=parity_indices(example,targets,128);probe=example[indices]
    with torch.no_grad(): expected=cpu_model(torch.from_numpy(probe)).numpy()
    actual=session.run(None,{'history':probe})[0]
    for _ in range(20): session.run(None,{'history':example[:1]})
    timings=[]
    for _ in range(200):
        start=time.perf_counter_ns(); session.run(None,{'history':example[:1]}); timings.append((time.perf_counter_ns()-start)/1000)
    return dict(onnx_bytes=path.stat().st_size,sha256=hashlib.sha256(path.read_bytes()).hexdigest(),
                onnx_max_absolute_error=float(np.max(np.abs(actual-expected))),
                cpu_latency_us_p50=float(np.median(timings)),cpu_latency_us_p95=float(np.percentile(timings,95)),
                parity_cases=len(probe),represented_mmr=sorted({round(float(x[-1,0])*10000) for x in probe}),
                discrete_positive_targets={OUTPUT_NAMES[i]:int(np.sum(targets[indices,i]>.5)) for i in (3,4,5)} if targets is not None else None,
                device='Windows host CPU, one thread; mobile performance unmeasured')


def export_native_mlp(model, example, output, targets=None):
    state=model.cpu().state_dict();data=b'ML02'+struct.pack('<5i',33,128,128,64,8)
    keys=('net.0.weight','net.0.bias','net.1.weight','net.1.bias','net.4.weight','net.4.bias',
        'net.5.weight','net.5.bias','net.8.weight','net.8.bias','net.10.weight','net.10.bias')
    for key in keys:data+=state[key].numpy().astype('<f4').tobytes()
    if len(data)!=120632:raise ValueError('Unexpected ML02 byte length.')
    path=output/'behavior_mlp.bytes';path.write_bytes(data)
    indices=parity_indices(example,targets,64);probe=example[indices]
    with torch.no_grad():expected=model(torch.from_numpy(probe)).numpy()
    fixture=dict(weights_sha256=hashlib.sha256(data).hexdigest(),
        sample_selection='All MMRs, rare commands, airborne state and whole held-out history',
        represented_mmr=sorted({round(float(x[-1,0])*10000) for x in probe}),
        cases=[dict(features=x[-1].tolist(),outputs=y.tolist()) for x,y in zip(probe,expected)])
    (output/'native-parity.json').write_text(json.dumps(fixture,indent=2,allow_nan=False),encoding='utf-8')
    return dict(bytes=len(data),sha256=fixture['weights_sha256'],format='ML02',authority='SHADOW_ONLY_EXPLICIT_QA_OPT_IN')


def run(data_dir,output,epochs=12,sequence_length=12,previous_meta=None,require_meta_drift=False):
    # Bound offline host work while CUDA handles training; do not monopolize the editor.
    torch.set_num_threads(8)
    output=Path(output); output.mkdir(parents=True,exist_ok=True)
    sessions,audit=load_dataset(data_dir)
    from meta_tech import report as tech_report
    tech=tech_report(sessions,output/'meta-tech-report.json',previous_meta)
    report=dict(status='LOCAL_CANDIDATE_ONLY',audit=audit,feature_version=FEATURE_VERSION,features=FEATURE_NAMES,
                targets=OUTPUT_NAMES,models={},promotion='SHADOW',promotion_reasons=[])
    report['target_alignment']='NEXT_TELEMETRY_COMMAND'
    report['meta_tech_clusters']=len(tech['clusters'])
    if require_meta_drift and not tech['retrain_scheduled']:
        report['status']='NO_VERIFIED_META_DRIFT'
        report['promotion_reasons'].append('No repeated, legal time-saving evidence above the configured confidence gates.')
        (output/'candidate-report.json').write_text(json.dumps(report,indent=2,allow_nan=False),encoding='utf-8')
        return report
    try:
        train,val,test,manifest=split_sessions(sessions)
        report['splits']=manifest
        report['quality_pace_references']=assign_training_pace_reference(train,(train,val,test))
        report['quality_scope']='Measured train-only lap and sector consistency, collisions, recoveries and route execution; never expert-human ground truth or an MMR reward.'
        prepared=[make_windows(part,sequence_length,return_persistence=True) for part in (train,val,test)]
        windows=[part[:3] for part in prepared]
        if any(len(part[0])==0 for part in windows): raise ValueError('Empty temporal split after continuity filtering.')
        report['samples_by_split']=[len(part[0]) for part in windows]
        report['held_out_persistence_baseline']=metrics(prepared[2][3],windows[2][1])
        delays=[b['timestamp']-a['timestamp'] for s in test for a,b in zip(s['samples'],s['samples'][1:])
                if 0<b['timestamp']-a['timestamp']<=.3]
        report['prediction_horizon_seconds_median']=float(np.median(delays))
        report['prediction_scope']='Next observed command; all state features precede the predicted action. Persistence is an explicit held-out reference.'
        report['human_sessions']=sum(s['metadata'].get('source')=='HUMAN_INPUT' for s in sessions)
        report['mmr_distribution']={str(m):sum(s['metadata']['playerMmr']==m for s in sessions) for m in sorted({s['metadata']['playerMmr'] for s in sessions})}
        device=torch.device('cuda' if torch.cuda.is_available() else 'cpu')
        report['training_device']=str(device)
        for kind in ('behavior_mlp','temporal_gru','compact_attention_gru'):
            model,history=train_candidate(kind,windows[0],windows[1],output,device,epochs,42)
            x,y,_=windows[2]; prediction=predict(model,x,device)
            result=metrics(prediction,y); result['history']=history
            result['held_out_metrics_by_mmr']={}
            for rating in (100,1000,2500,5000,7500,9000,9700,10000):
                mask=np.isclose(x[:,-1,0],rating/10000,atol=1e-6)
                if mask.any():result['held_out_metrics_by_mmr'][str(rating)]=dict(samples=int(mask.sum()),**metrics(prediction[mask],y[mask]))
            # Permutation ablation preserves observed feature ranges and tests usefulness.
            result['feature_permutation_delta_mae']={}
            base=float(np.abs(prediction-y).mean()); generator=np.random.default_rng(42)
            for group,indices in FEATURE_GROUPS.items():
                permuted=x.copy(); order=generator.permutation(len(x))
                permuted[:,:,indices]=x[order][:,:,indices]
                result['feature_permutation_delta_mae'][group]=float(np.abs(predict(model,permuted,device)-y).mean()-base)
            result['same_scenario_mmr_sweep']={}
            for rating in (100,1000,2500,5000,7500,9000,9700,10000):
                probe=x[:1].copy(); probe[:,:,0]=np.clip(rating/10000,0,1)
                result['same_scenario_mmr_sweep'][str(rating)]=predict(model,probe,device)[0].tolist()
            result['conditioning_claim']='Response sensitivity only; skill competence requires held-out MMR behavior and competitive human race comparison.'
            result['runtime']=evaluate_runtime(model,x,output/(kind+'.onnx'),y)
            if kind=='behavior_mlp':result['native']=export_native_mlp(model,x,output,y)
            report['models'][kind]=result
            model.to(device)
        if not report['human_sessions']: report['promotion_reasons'].append('No verified human expert demonstrations.')
        report['promotion_reasons'].append('No closed-loop assist/authority safety or expert ghost gates; candidates are not installed.')
        report['recommended_candidate']=min(report['models'],key=lambda k:report['models'][k]['runtime']['cpu_latency_us_p95'])
        report['selection_scope']='Latency candidate for further Shadow testing, not best competitive driver.'
    except ValueError as ex:
        report['status']='BLOCKED_BY_DATA_GATE'; report['promotion_reasons'].append(str(ex))
    (output/'candidate-report.json').write_text(json.dumps(report,indent=2,allow_nan=False),encoding='utf-8')
    return report


if __name__=='__main__':
    parser=argparse.ArgumentParser()
    parser.add_argument('--data-dir',default='Tools/ML/Data'); parser.add_argument('--output',required=True)
    parser.add_argument('--epochs',type=int,default=12); parser.add_argument('--sequence-length',type=int,default=12)
    parser.add_argument('--previous-meta');parser.add_argument('--only-on-meta-drift',action='store_true')
    args=parser.parse_args(); result=run(args.data_dir,args.output,args.epochs,args.sequence_length,args.previous_meta,args.only_on_meta_drift)
    print(result['status'],result['promotion_reasons'])
    raise SystemExit(0 if result['models'] or result['status']=='NO_VERIFIED_META_DRIFT' else 2)
