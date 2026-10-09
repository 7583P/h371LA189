import copy
import json
import tempfile
from pathlib import Path
import unittest
from unittest.mock import patch
import numpy as np
import torch
from racing_dataset import (validate_session, feature_vector, target_vector, trajectory_hash,
    demonstration_quality, make_windows, split_sessions, FEATURE_VERSION,assign_training_pace_reference,legacy_item_family)
from release_pipeline import metrics,ContextMLP,export_native_mlp,parity_indices
from meta_tech import extract_occurrences,report as tech_report


def fixture(seed=1,mmr=1000):
    samples=[dict(timestamp=i*.1,trackProgress=i*.001,trackDistance=i,
        worldPosition=dict(x=0.,y=1.,z=float(i)),velocity=dict(x=0.,y=0.,z=10.),
        speedMps=10.,speedKmh=36.,steering=.1,throttle=.8,brake=0.,pitch=0.,roll=0.,yaw=0.,
        jumpInput=False,isAirborne=False,driftInput=False,aerialBoostInput=False,pitchInput=0.,airRollInput=0.,
        localVelocity=dict(x=0.,y=0.,z=10.),angularVelocity=dict(x=0.,y=0.,z=0.),lateralOffset=0.,distanceToLeadRacer=0.,
        headingError=0.,curvature15=0.,curvature35=0.,curvature70=0.,slope=0.,bankAngle=0.,driftCharge=0.,boostRemaining=0.,
        nextJumpDistance=200.,checkpointProgress=0.,racePosition=1,gapAhead=100.,gapBehind=100.,relativeSpeedAhead=0.,
        activeItemType=0,nearbyRacerCount=0) for i in range(24)]
    s=dict(metadata=dict(schemaVersion='2.0.0',source='SCRIPTED_UNITY_PHYSICS',featureVersion=FEATURE_VERSION,actionLabelVersion='command-intent-1',
        isLegallyValidated=True,isHumanPlayer=False,sessionId=str(seed),trackId='NightCityGrandCircuit',trackRevision='fixture',
        trackSeed=seed,playerMmr=mmr,racerCount=12,bestLapTime=.5,lapTimes=[1.,.7,.5]),samples=samples,events=[])
    s['_fingerprint']=trajectory_hash(s)
    return s


class ReleasePipelineTests(unittest.TestCase):
    def test_valid_fixture(self): self.assertEqual(validate_session(fixture()),[])
    def test_nonfinite_first_sample_rejected(self):
        s=fixture();s['samples'][0]['speedMps']=float('nan');self.assertTrue(validate_session(s))
    def test_duplicate_time_rejected(self):
        s=fixture();s['samples'][1]['timestamp']=0;self.assertTrue(validate_session(s))
    def test_human_provenance_rejected(self):
        s=fixture();s['metadata']['isHumanPlayer']=True;self.assertTrue(validate_session(s))
    def test_human_source_requires_human_identity(self):
        s=fixture();s['metadata']['source']='HUMAN_INPUT';self.assertTrue(validate_session(s))
    def test_missing_context_feature_rejected(self):
        s=fixture();del s['samples'][0]['curvature70'];self.assertTrue(validate_session(s))
    def test_unversioned_command_labels_rejected(self):
        s=fixture();del s['metadata']['actionLabelVersion'];self.assertTrue(validate_session(s))
    def test_false_lap_claim_rejected(self):
        s=fixture();s['metadata']['bestLapTime']=24.75;self.assertTrue(validate_session(s))
    def test_laps_cannot_exceed_recorded_session_clock(self):
        s=fixture();s['metadata']['lapTimes']=[50.,51.,52.];s['metadata']['bestLapTime']=50.;self.assertTrue(validate_session(s))
    def test_teleport_requires_recovery(self):
        s=fixture();s['samples'][3]['worldPosition']['x']=100;self.assertTrue(validate_session(s))
        s['events']=[dict(timestamp=.3,eventType=9,tag='Recovery')];self.assertEqual(validate_session(s),[])
    def test_duplicate_trajectory_ignores_provenance_metadata(self):
        a=fixture();b=fixture(2,100000);self.assertEqual(trajectory_hash(a),trajectory_hash(b))
    def test_quality_does_not_reward_mmr(self):
        self.assertEqual(demonstration_quality(fixture(mmr=1000)),demonstration_quality(fixture(mmr=100000)))
    def test_quality_pace_reference_uses_training_only(self):
        train,test=fixture(),fixture(2)
        train['metadata']['lapTimes']=[1.,1.,1.];test['metadata']['lapTimes']=[.1,.1,.1]
        assign_training_pace_reference([train],([train],[test]))
        self.assertEqual(test['_training_pace_reference'],1.)
        slower=copy.deepcopy(train);slower['metadata']['lapTimes']=[2.,2.,2.]
        self.assertGreater(demonstration_quality(train),demonstration_quality(slower))
    def test_failed_legality_quality_low(self):
        s=fixture();s['metadata']['isLegallyValidated']=False;self.assertLessEqual(demonstration_quality(s),.05)
    def test_action_uses_intent_not_airborne_state(self):
        s=fixture()['samples'][0];s['isAirborne']=True;self.assertEqual(target_vector(s)[4],0)
    def test_features_mmr_only_changes_condition(self):
        s=fixture()['samples'][0];a=feature_vector(s,1000);b=feature_vector(s,100000)
        self.assertEqual(len(a),33);np.testing.assert_array_equal(a[1:],b[1:]);self.assertEqual(b[0],1)
    def test_current_10k_conditioning_and_archived_v2_are_explicit(self):
        sample=fixture()['samples'][0]
        for rating in (0,100,1000,2500,5000,7500,9000,9700,10000,20000):
            self.assertAlmostEqual(float(feature_vector(sample,rating)[0]),min(1,rating/10000),places=6)
        self.assertEqual(feature_vector(sample,1000,schema_version='2.0.0')[0],0)
        self.assertEqual(feature_vector(sample,100000,schema_version='2.0.0')[0],1)
    def test_current_schema_requires_explicit_scale_and_correct_normalization(self):
        s=fixture(mmr=0);s['metadata'].update(schemaVersion='3.0.0',ratingScaleMaximum=10000,ratingNormalizationVersion='mmr-0-10k-1',normalizedSkill=0)
        self.assertEqual(validate_session(s),[])
        s['metadata']['normalizedSkill']=1;self.assertTrue(validate_session(s))
        s['metadata'].update(playerMmr=15000,normalizedSkill=1);self.assertEqual(validate_session(s),[])
        del s['metadata']['ratingScaleMaximum'];self.assertTrue(validate_session(s))
    def test_items_keep_installed_mechanical_encoding(self):
        sample=fixture()['samples'][0]
        expected=[0,1,2,3,4,5,6,3,1,3,2,2,6,0,1,1]
        baseline=feature_vector(sample,100000)
        for item,family in enumerate(expected):
            with self.subTest(item=item):
                self.assertEqual(legacy_item_family(item),family)
                encoded=feature_vector(dict(sample,activeItemType=item),100000)
                self.assertEqual(len(encoded),33)
                self.assertAlmostEqual(float(encoded[31]),family/6,places=6)
                np.testing.assert_array_equal(np.delete(encoded,31),np.delete(baseline,31))
        self.assertEqual(legacy_item_family(-1),0)
        self.assertEqual(legacy_item_family(999),0)
    def test_temporal_final_target_included(self):
        s=fixture();s['samples'][-1]['steering']=-.7
        x,y,_=make_windows([s],12)
        self.assertEqual(len(x),12);self.assertAlmostEqual(y[-1,0],-.7)
    def test_state_precedes_predicted_command(self):
        s=fixture();s['samples'][12]['jumpInput']=True;s['samples'][12]['isAirborne']=True
        x,y,_,baseline=make_windows([s],12,return_persistence=True)
        self.assertEqual(x[0,-1,8],0);self.assertEqual(y[0,4],1);self.assertEqual(baseline[0,4],0)
    def test_temporal_gap_excludes_crossing_windows(self):
        s=fixture();s['samples'][12]['timestamp']+=.5;self.assertLess(len(make_windows([s],12)[0]),12)
    def test_temporal_recovery_excludes_crossing_windows(self):
        s=fixture();s['events']=[dict(timestamp=1.2,eventType=9)];self.assertLess(len(make_windows([s],12)[0]),12)
    def test_insufficient_seed_groups_rejected(self):
        with self.assertRaises(ValueError):split_sessions([fixture(),fixture(2)])
    def test_whole_race_seed_cannot_cross_splits(self):
        a,b,c,manifest=split_sessions([fixture(seed=i) for i in range(1,7) for _ in range(2)])
        sets=[{s['metadata']['trackSeed'] for s in x} for x in (a,b,c)]
        self.assertFalse(sets[0]&sets[1] or sets[1]&sets[2] or sets[0]&sets[2])
    def test_absent_action_support_never_fake_perfect_f1(self):
        result=metrics(np.zeros((5,8)),np.zeros((5,8)))
        self.assertIsNone(result['discrete']['aerial_boost']['f1'])
    def test_tech_has_no_invented_time_saving(self):
        s=fixture();s['samples'][3]['isAirborne']=True;s['samples'][4]['isAirborne']=True
        s['events']=[dict(timestamp=.5,eventType=3)]
        occurrence=extract_occurrences(s)[0];self.assertIsNone(occurrence['time_saved']);self.assertTrue(occurrence['success'])
    def test_tech_ids_independent_of_session_and_mmr(self):
        a=fixture();a['samples'][3]['isAirborne']=True;a['events']=[dict(timestamp=.4,eventType=3)]
        b=copy.deepcopy(a);b['metadata']['sessionId']='other';b['metadata']['playerMmr']=100000
        self.assertEqual(extract_occurrences(a)[0]['tech_id'],extract_occurrences(b)[0]['tech_id'])
    def test_road_bounce_is_not_a_commanded_jump(self):
        s=fixture();s['samples'][3]['isAirborne']=True
        self.assertIn('NaturalAirborne',extract_occurrences(s)[0]['actions'])
        s['events']=[dict(timestamp=.3,eventType=1,tag='AutomaticLaunch')]
        self.assertIn('LaunchPad',extract_occurrences(s)[0]['actions'])
    def test_repeated_commanded_landing_chain_is_measured(self):
        s=fixture()
        for i in (3,4,8,9):s['samples'][i]['isAirborne']=True
        s['samples'][3]['jumpInput']=True;s['samples'][8]['jumpInput']=True
        s['events']=[dict(timestamp=.5,eventType=3),dict(timestamp=1.,eventType=3)]
        chains=[x for x in extract_occurrences(s) if x['flights']==2]
        self.assertEqual(len(chains),1);self.assertTrue(chains[0]['success'])
        self.assertIsNone(chains[0]['time_saved']);self.assertAlmostEqual(chains[0]['duration'],.8)
    def test_one_lucky_tech_never_schedules_retraining(self):
        s=fixture();s['samples'][3]['isAirborne']=True;s['events']=[dict(timestamp=.4,eventType=3)]
        with tempfile.TemporaryDirectory() as folder:
            prior=Path(folder)/'prior.json';prior.write_text('{"clusters":[]}')
            result=tech_report([s],Path(folder)/'current.json',prior)
            self.assertFalse(result['retrain_scheduled']);self.assertFalse(result['clusters'][0]['promotion_eligible'])
    def test_native_context_export_has_exact_layout_and_hash(self):
        model=ContextMLP(input_dim=33,output_dim=8).eval();examples=np.zeros((64,12,33),dtype=np.float32)
        with tempfile.TemporaryDirectory() as folder:
            result=export_native_mlp(model,examples,Path(folder));data=(Path(folder)/'behavior_mlp.bytes').read_bytes()
            self.assertEqual(data[:4],b'ML02');self.assertEqual(len(data),120632)
            parity=json.loads((Path(folder)/'native-parity.json').read_text())
            self.assertEqual(len(parity['cases']),64);self.assertEqual(len(parity['cases'][0]['outputs']),8)
            self.assertEqual(result['sha256'],parity['weights_sha256'])
    def test_parity_probe_covers_all_ratings_and_rare_actions(self):
        x=np.zeros((256,12,33),dtype=np.float32);y=np.zeros((256,8),dtype=np.float32)
        ratings=np.asarray([1000,10000,25000,50000,75000,90000,97000,100000])
        x[:,:,0]=np.repeat((ratings-1000)/99000,32)[:,None]
        y[80:100,3]=1;y[[30,120,200],4]=1;y[[4,110,205],5]=1
        selected=parity_indices(x,y,64)
        self.assertEqual(len(selected),64)
        self.assertEqual(len(np.unique(x[selected,-1,0])),8)
        self.assertTrue(all(np.any(y[selected,i]>.5) for i in (3,4,5)))
    def test_sub_sample_saving_cannot_schedule_retraining(self):
        self.assertFalse(self._meta_saving_fixture(.035)['retrain_scheduled'])
    def test_repeated_saving_above_resolution_can_schedule_local_candidate(self):
        result=self._meta_saving_fixture(.2)
        self.assertTrue(result['retrain_scheduled']);self.assertEqual(result['authority'],'SHADOW')
    def _meta_saving_fixture(self,saving):
        sessions=[fixture(seed=i) for i in (1,2,3)]
        for s in sessions:
            for i in (3,10):
                s['samples'][i]['isAirborne']=True;s['samples'][i]['jumpInput']=True
                s['events'].append(dict(timestamp=(i+1)*.1,eventType=3))
        def matched(occurrences,_):
            for o in occurrences:o.update(matched_baseline=True,time_saved=saving)
        with tempfile.TemporaryDirectory() as folder,patch('meta_tech.match_baselines',side_effect=matched):
            prior=Path(folder)/'prior.json';prior.write_text('{"clusters":[]}')
            return tech_report(sessions,Path(folder)/'current.json',prior)


if __name__=='__main__':unittest.main(verbosity=2)
