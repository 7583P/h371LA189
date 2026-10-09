#!/usr/bin/env python3
"""
Exports trained Behavior Cloning MLP weights to binary format for Unity runtime inference.
Supports zero-allocation native C# evaluation in RacingMlInferenceAdapter.
"""

import os
import sys
import struct
from pathlib import Path
import numpy as np
import torch

repo_root = Path(__file__).resolve().parent.parent.parent
if str(repo_root) not in sys.path:
    sys.path.insert(0, str(repo_root))

from Tools.ML.train_behavior import BehaviorCloningMLP

def export_unity_binary_weights(
    ckpt_path="Tools/ML/Models/behavior_baseline.pt",
    output_path="Assets/AnimeHillRacing/Resources/RacingMlWeights.bytes"
):
    if not os.path.exists(ckpt_path):
        print(f"Checkpoint not found: {ckpt_path}")
        return False

    ckpt = torch.load(ckpt_path, map_location="cpu", weights_only=True)
    state = ckpt["model_state_dict"]

    w0 = state["net.0.weight"].numpy() # [128, 11]
    b0 = state["net.0.bias"].numpy()   # [128]
    ln0_w = state["net.1.weight"].numpy() # [128]
    ln0_b = state["net.1.bias"].numpy()   # [128]

    w4 = state["net.4.weight"].numpy() # [128, 128]
    b4 = state["net.4.bias"].numpy()   # [128]
    ln4_w = state["net.5.weight"].numpy() # [128]
    ln4_b = state["net.5.bias"].numpy()   # [128]

    w8 = state["net.8.weight"].numpy() # [64, 128]
    b8 = state["net.8.bias"].numpy()   # [64]

    w10 = state["net.10.weight"].numpy() # [5, 64]
    b10 = state["net.10.bias"].numpy()   # [5]

    os.makedirs(os.path.dirname(output_path), exist_ok=True)
    with open(output_path, "wb") as fp:
        # Magic header: ML01
        fp.write(b"ML01")
        # Dims: 11, 128, 128, 64, 5
        fp.write(struct.pack("<5i", 11, 128, 128, 64, 5))

        # Write layer 0
        w0.astype(np.float32).tofile(fp)
        b0.astype(np.float32).tofile(fp)
        ln0_w.astype(np.float32).tofile(fp)
        ln0_b.astype(np.float32).tofile(fp)

        # Write layer 4
        w4.astype(np.float32).tofile(fp)
        b4.astype(np.float32).tofile(fp)
        ln4_w.astype(np.float32).tofile(fp)
        ln4_b.astype(np.float32).tofile(fp)

        # Write layer 8
        w8.astype(np.float32).tofile(fp)
        b8.astype(np.float32).tofile(fp)

        # Write layer 10
        w10.astype(np.float32).tofile(fp)
        b10.astype(np.float32).tofile(fp)

    file_size_kb = os.path.getsize(output_path) / 1024.0
    print(f"Exported Unity binary weights to: {output_path} ({file_size_kb:.2f} KB)")

    # Test parity on test vector
    test_in = np.ones((1, 11), dtype=np.float32) * 0.5
    with torch.no_grad():
        model = BehaviorCloningMLP(input_dim=11, output_dim=5)
        model.load_state_dict(state)
        model.eval()
        py_out = model(torch.from_numpy(test_in)).numpy()[0]
    print(f"Reference Python Forward Output: {py_out}")
    return True

if __name__ == "__main__":
    export_unity_binary_weights()
