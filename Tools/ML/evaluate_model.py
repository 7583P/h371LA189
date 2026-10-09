"""Held-out evaluation only. Legacy weights have no trustworthy split manifest."""
import argparse
import json
from pathlib import Path
import torch
from racing_dataset import load_dataset, make_windows, FEATURE_VERSION
from release_pipeline import ContextMLP, ContextGRU, CompactAttentionGRU, metrics, predict


def evaluate_models(data_dir="Tools/ML/Data", models_dir="Tools/ML/Models"):
    torch.set_num_threads(8)
    directory=Path(models_dir)
    manifest_path=directory/'candidate-report.json'
    if not manifest_path.exists():
        raise ValueError('No held-out candidate manifest. Legacy evaluation on all training sessions is ineligible; run release_pipeline.py into a candidate directory.')
    manifest=json.loads(manifest_path.read_text(encoding='utf-8'))
    sessions,audit=load_dataset(data_dir)
    split=manifest['splits']
    train=set(split['train']['trajectories']); validation=set(split['validation']['trajectories']); test=set(split['test']['trajectories'])
    if train&test or validation&test or train&validation: raise ValueError('Overlapping split trajectories.')
    rows=[s for s in sessions if s['_fingerprint'] in test]
    if {s['_fingerprint'] for s in rows}!=test: raise ValueError('Test data changed since training manifest.')
    factories={'behavior_mlp':ContextMLP,'temporal_gru':ContextGRU,'compact_attention_gru':CompactAttentionGRU}
    result={'scope':'HELD_OUT_TEST_ONLY','audit':audit,'models':{}}
    for kind in manifest['models']:
        checkpoint=torch.load(directory/(kind+'.pt'),map_location='cpu',weights_only=True)
        if checkpoint.get('feature_version')!=FEATURE_VERSION: raise ValueError('Feature version mismatch.')
        if checkpoint.get('target_alignment')!='NEXT_TELEMETRY_COMMAND':raise ValueError('Unverified causal target alignment.')
        model=factories[kind](checkpoint['input_dim'],output_dim=checkpoint['output_dim']).eval()
        model.load_state_dict(checkpoint['model_state_dict'])
        x,y,_=make_windows(rows,checkpoint['sequence_length'])
        result['models'][kind]=metrics(predict(model,x,'cpu'),y)
    (directory/'held-out-evaluation.json').write_text(json.dumps(result,indent=2,allow_nan=False),encoding='utf-8')
    return result


if __name__=='__main__':
    parser=argparse.ArgumentParser(); parser.add_argument('--data-dir',default='Tools/ML/Data'); parser.add_argument('--models-dir',required=True)
    args=parser.parse_args()
    try: print(json.dumps(evaluate_models(args.data_dir,args.models_dir),indent=2))
    except ValueError as error: print('EVALUATION_REJECTED:',error); raise SystemExit(2)
