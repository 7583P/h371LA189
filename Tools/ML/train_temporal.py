#!/usr/bin/env python3
"""
Model B: Temporal Driving Model for Hill Adventurer Racing 3D.
Uses a causal GRU sequence model over recent history (1.5 - 2.0s window) to predict driving actions.
Supports CUDA GPU and CPU fallback.
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

class TemporalTelemetryDataset(Dataset):
    def __init__(self, sessions, sequence_length=20):
        self.sequences = []
        self.targets = []
        self.weights = []
        self.seq_len = sequence_length

        for s in sessions:
            meta = s.get("metadata", {})
            raw_samples = s.get("samples", [])
            if len(raw_samples) <= sequence_length:
                continue

            mmr = float(meta.get("playerMmr", 0))
            skill = normalized_mmr(mmr, meta.get("schemaVersion", "2.0.0"))
            q_score = float(meta.get("trainingQualityScore", 0.7))

            feats = []
            tgts = []
            for raw in raw_samples:
                f = [
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
                t = [
                    float(raw.get("steering", 0.0)),
                    float(raw.get("throttle", 1.0)),
                    float(raw.get("brake", 0.0)),
                    1.0 if raw.get("isDrifting", False) else 0.0,
                    1.0 if raw.get("isJumping", False) else 0.0
                ]
                feats.append(f)
                tgts.append(t)

            # Build sliding windows
            for i in range(len(feats) - sequence_length):
                self.sequences.append(feats[i : i + sequence_length])
                self.targets.append(tgts[i + sequence_length - 1])
                self.weights.append(q_score)

        self.sequences = np.array(self.sequences, dtype=np.float32)
        self.targets = np.array(self.targets, dtype=np.float32)
        self.weights = np.array(self.weights, dtype=np.float32)

    def __len__(self):
        return len(self.sequences)

    def __getitem__(self, idx):
        return (
            torch.from_numpy(self.sequences[idx]),
            torch.from_numpy(self.targets[idx]),
            torch.tensor(self.weights[idx], dtype=torch.float32)
        )


class TemporalDrivingGRU(nn.Module):
    def __init__(self, input_dim=11, hidden_dim=64, num_layers=2, output_dim=5):
        super().__init__()
        self.hidden_dim = hidden_dim
        self.num_layers = num_layers
        self.gru = nn.GRU(
            input_size=input_dim,
            hidden_size=hidden_dim,
            num_layers=num_layers,
            batch_first=True,
            dropout=0.1 if num_layers > 1 else 0.0
        )
        self.fc = nn.Sequential(
            nn.Linear(hidden_dim, 64),
            nn.ReLU(),
            nn.Linear(64, output_dim)
        )

    def forward(self, x, h=None):
        out, h_n = self.gru(x, h)
        last_step = out[:, -1, :] # Last timestep
        raw = self.fc(last_step)
        
        steer = torch.tanh(raw[..., 0:1])
        throttle = torch.sigmoid(raw[..., 1:2])
        brake = torch.sigmoid(raw[..., 2:3])
        drift = torch.sigmoid(raw[..., 3:4])
        jump = torch.sigmoid(raw[..., 4:5])
        return torch.cat([steer, throttle, brake, drift, jump], dim=-1)


def train_temporal_model(data_dir="Tools/ML/Data", models_dir="Tools/ML/Models", epochs=20, lr=0.001, batch_size=64, seq_len=20):
    """Compatibility entry point: guarded candidates, never overwrites installed models."""
    from datetime import datetime, timezone
    from release_pipeline import run
    output = Path(models_dir)/"Candidates"/datetime.now(timezone.utc).strftime("%Y%m%dT%H%M%SZ")
    return run(data_dir, output, epochs=epochs, sequence_length=seq_len)


if __name__ == "__main__":
    import argparse
    parser=argparse.ArgumentParser(description="Validated local candidate comparison; runtime remains Shadow.")
    parser.add_argument("--data-dir",default="Tools/ML/Data")
    parser.add_argument("--models-dir",default="Tools/ML/Models")
    parser.add_argument("--epochs",type=int,default=12)
    args=parser.parse_args()
    result=train_temporal_model(args.data_dir,args.models_dir,args.epochs)
    print(result["status"],result["promotion_reasons"])
    raise SystemExit(0 if result["models"] else 2)
