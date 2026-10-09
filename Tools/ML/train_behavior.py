#!/usr/bin/env python3
"""
Model A: Skill-Conditioned Behavior Cloning for Hill Adventurer Racing 3D.
Archived baseline tool. Current conditioning is 0–10k; old sessions keep explicit v2 input compatibility.
Supports GPU (CUDA) with seamless CPU fallback.
"""

import os
import sys
import glob
import json
import random
from pathlib import Path
import numpy as np
import torch
import torch.nn as nn
import torch.optim as optim
from torch.utils.data import Dataset, DataLoader
from racing_dataset import normalized_mmr

class RacingTelemetryDataset(Dataset):
    def __init__(self, sessions, sequence_length=1):
        self.samples = []
        self.targets = []
        self.weights = []
        self.sequence_length = sequence_length

        for s in sessions:
            meta = s.get("metadata", {})
            raw_samples = s.get("samples", [])
            if len(raw_samples) < 5:
                continue

            mmr = float(meta.get("playerMmr", 0))
            skill = normalized_mmr(mmr, meta.get("schemaVersion", "2.0.0"))
            q_score = float(meta.get("trainingQualityScore", 0.7))

            for raw in raw_samples:
                # 11-dimensional input feature vector
                feat = [
                    skill,
                    np.clip(float(raw.get("speedMps", 0.0)) / 50.0, 0.0, 2.0),
                    float(raw.get("velocity", {}).get("x", 0.0)) / 50.0,
                    float(raw.get("velocity", {}).get("y", 0.0)) / 50.0,
                    float(raw.get("velocity", {}).get("z", 0.0)) / 50.0,
                    float(raw.get("pitch", 0.0)) / 90.0,
                    float(raw.get("roll", 0.0)) / 90.0,
                    float(raw.get("yaw", 0.0)) / 180.0,
                    1.0 if raw.get("isAirborne", False) else 0.0,
                    float(raw.get("lateralOffset", 0.0)) / 15.0,
                    np.clip(float(raw.get("distanceToLeadRacer", 0.0)) / 50.0, 0.0, 2.0)
                ]

                # 5-dimensional target vector: [steer, throttle, brake, drift, jump]
                tgt = [
                    float(raw.get("steering", 0.0)),
                    float(raw.get("throttle", 1.0)),
                    float(raw.get("brake", 0.0)),
                    1.0 if raw.get("isDrifting", False) else 0.0,
                    1.0 if raw.get("isJumping", False) else 0.0
                ]

                self.samples.append(feat)
                self.targets.append(tgt)
                self.weights.append(q_score)

        self.samples = np.array(self.samples, dtype=np.float32)
        self.targets = np.array(self.targets, dtype=np.float32)
        self.weights = np.array(self.weights, dtype=np.float32)

    def __len__(self):
        return len(self.samples)

    def __getitem__(self, idx):
        return (
            torch.from_numpy(self.samples[idx]),
            torch.from_numpy(self.targets[idx]),
            torch.tensor(self.weights[idx], dtype=torch.float32)
        )


class BehaviorCloningMLP(nn.Module):
    def __init__(self, input_dim=11, hidden_dim=128, output_dim=5):
        super().__init__()
        self.net = nn.Sequential(
            nn.Linear(input_dim, hidden_dim),
            nn.LayerNorm(hidden_dim),
            nn.ReLU(),
            nn.Dropout(0.05),
            nn.Linear(hidden_dim, hidden_dim),
            nn.LayerNorm(hidden_dim),
            nn.ReLU(),
            nn.Dropout(0.05),
            nn.Linear(hidden_dim, 64),
            nn.ReLU(),
            nn.Linear(64, output_dim)
        )

    def forward(self, x):
        raw = self.net(x)
        # Apply output constraints:
        # steer: [-1, 1], throttle: [0, 1], brake: [0, 1], drift: [0, 1], jump: [0, 1]
        steer = torch.tanh(raw[..., 0:1])
        throttle = torch.sigmoid(raw[..., 1:2])
        brake = torch.sigmoid(raw[..., 2:3])
        drift = torch.sigmoid(raw[..., 3:4])
        jump = torch.sigmoid(raw[..., 4:5])
        return torch.cat([steer, throttle, brake, drift, jump], dim=-1)


def train_behavior_cloning(data_dir="Tools/ML/Data", models_dir="Tools/ML/Models", epochs=20, lr=0.001, batch_size=64):
    """Compatibility entry point: guarded candidates, never overwrites installed models."""
    from datetime import datetime, timezone
    from release_pipeline import run
    output = Path(models_dir)/"Candidates"/datetime.now(timezone.utc).strftime("%Y%m%dT%H%M%SZ")
    return run(data_dir, output, epochs=epochs, sequence_length=12)


if __name__ == "__main__":
    import argparse
    parser=argparse.ArgumentParser(description="Validated local candidate comparison; runtime remains Shadow.")
    parser.add_argument("--data-dir",default="Tools/ML/Data")
    parser.add_argument("--models-dir",default="Tools/ML/Models")
    parser.add_argument("--epochs",type=int,default=12)
    args=parser.parse_args()
    result=train_behavior_cloning(args.data_dir,args.models_dir,args.epochs)
    print(result["status"],result["promotion_reasons"])
    raise SystemExit(0 if result["models"] else 2)
