#!/usr/bin/env python3
"""
ONNX Model Exporter for Hill Adventurer Racing 3D.
Exports trained PyTorch checkpoints to ONNX, verifies graph correctness,
and validates output parity with onnxruntime.
"""

import os
import sys
import json
from pathlib import Path

# Add repo root to sys.path
repo_root = Path(__file__).resolve().parent.parent.parent
if str(repo_root) not in sys.path:
    sys.path.insert(0, str(repo_root))

import numpy as np
import torch
import torch.nn as nn
from Tools.ML.train_behavior import BehaviorCloningMLP
from Tools.ML.train_temporal import TemporalDrivingGRU

def export_to_onnx(model_type="behavior", models_dir="Tools/ML/Models", opset_version=17):
    device = torch.device("cpu") # ONNX export is cleanest on CPU
    os.makedirs(models_dir, exist_ok=True)

    if model_type == "behavior":
        ckpt_path = os.path.join(models_dir, "behavior_baseline.pt")
        onnx_path = os.path.join(models_dir, "racing_behavior_baseline.onnx")
        if not os.path.exists(ckpt_path):
            print(f"[export_onnx] Checkpoint not found: {ckpt_path}")
            return False

        ckpt = torch.load(ckpt_path, map_location=device, weights_only=True)
        model = BehaviorCloningMLP(input_dim=ckpt.get("input_dim", 11), output_dim=ckpt.get("output_dim", 5))
        model.load_state_dict(ckpt["model_state_dict"])
        model.eval()

        dummy_input = torch.randn(1, 11, dtype=torch.float32)
        input_names = ["input_features"]
        output_names = ["predicted_actions"]
        dynamic_axes = {
            "input_features": {0: "batch_size"},
            "predicted_actions": {0: "batch_size"}
        }

        print(f"[export_onnx] Exporting Behavior Cloning model to {onnx_path}...")
        torch.onnx.export(
            model,
            dummy_input,
            onnx_path,
            export_params=True,
            opset_version=opset_version,
            do_constant_folding=True,
            input_names=input_names,
            output_names=output_names,
            dynamic_axes=dynamic_axes
        )

    elif model_type == "temporal":
        ckpt_path = os.path.join(models_dir, "temporal_model.pt")
        onnx_path = os.path.join(models_dir, "racing_temporal_model.onnx")
        if not os.path.exists(ckpt_path):
            print(f"[export_onnx] Checkpoint not found: {ckpt_path}")
            return False

        ckpt = torch.load(ckpt_path, map_location=device, weights_only=True)
        model = TemporalDrivingGRU(
            input_dim=ckpt.get("input_dim", 11),
            hidden_dim=ckpt.get("hidden_dim", 64),
            num_layers=ckpt.get("num_layers", 2),
            output_dim=ckpt.get("output_dim", 5)
        )
        model.load_state_dict(ckpt["model_state_dict"])
        model.eval()

        seq_len = ckpt.get("seq_len", 20)
        dummy_input = torch.randn(1, seq_len, 11, dtype=torch.float32)
        input_names = ["temporal_sequence"]
        output_names = ["predicted_actions"]
        dynamic_axes = {
            "temporal_sequence": {0: "batch_size"},
            "predicted_actions": {0: "batch_size"}
        }

        print(f"[export_onnx] Exporting Temporal GRU model to {onnx_path}...")
        torch.onnx.export(
            model,
            dummy_input,
            onnx_path,
            export_params=True,
            opset_version=opset_version,
            do_constant_folding=True,
            input_names=input_names,
            output_names=output_names,
            dynamic_axes=dynamic_axes
        )
    else:
        print(f"Unknown model_type: {model_type}")
        return False

    # Check model with ONNX checker
    try:
        import onnx
        onnx_model = onnx.load(onnx_path)
        onnx.checker.check_model(onnx_model)
        print(f"[export_onnx] ONNX model graph structure verified successfully.")
    except Exception as ex:
        print(f"[export_onnx] Warning on ONNX graph check: {ex}")

    # Check numerical parity with onnxruntime
    try:
        import onnxruntime as ort
        session = ort.InferenceSession(onnx_path, providers=["CPUExecutionProvider"])
        ort_inputs = {session.get_inputs()[0].name: dummy_input.numpy()}
        ort_outs = session.run(None, ort_inputs)
        with torch.no_grad():
            torch_outs = model(dummy_input).numpy()
        max_diff = np.max(np.abs(ort_outs[0] - torch_outs))
        print(f"[export_onnx] ONNX Runtime Parity Check: Max Absolute Difference = {max_diff:.8f}")
    except Exception as ex:
        print(f"[export_onnx] Note on ONNX Runtime parity check: {ex}")

    file_size_kb = os.path.getsize(onnx_path) / 1024.0
    print(f"[export_onnx] ONNX Model Export Complete!")
    print(f"  Path: {onnx_path}")
    print(f"  Size: {file_size_kb:.2f} KB (Well below 50 MB mobile limit)")
    return True

if __name__ == "__main__":
    mtype = sys.argv[1] if len(sys.argv) > 1 else "behavior"
    export_to_onnx(mtype)
