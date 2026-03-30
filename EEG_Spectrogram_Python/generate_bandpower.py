# ==========================================
# generate_bandpower.py
# ------------------------------------------
# EEGの各周波数帯域（δ, θ, α, β, γ）のパワー変化を線グラフで可視化
# ==========================================

import numpy as np
import pandas as pd
import matplotlib.pyplot as plt

# ===============================
# 1. パラメータ設定
# ===============================
fs = 1000           # サンプリング周波数 [Hz]
nperseg = 1024       # 窓長
noverlap = 512       # オーバーラップ
window = np.hamming(nperseg)

# 各帯域の表示ON/OFF設定
show_delta = False    # δ波 (0–4 Hz)
show_theta = False    # θ波 (4–8 Hz)
show_alpha = False    # α波 (8–13 Hz)
show_beta  = True    # β波 (13–36 Hz)
show_gamma = True    # γ波 (36Hz 以上)

# ===============================
# 2. データ読み込み
# ===============================
df = pd.read_csv("signal_data.csv")
signal = df["Signal Raw [uV]"].values
N = len(signal)

# ===============================
# 3. フレーム分割とDFT
# ===============================
hop = nperseg - noverlap
num_frames = (N - nperseg) // hop + 1
freqs = np.fft.rfftfreq(nperseg, 1/fs)
times = np.arange(num_frames) * hop / fs

Sxx = np.zeros((len(freqs), num_frames))
win_power = np.sum(window**2)

for i in range(num_frames):
    start = i * hop
    segment = signal[start:start + nperseg]
    if len(segment) < nperseg:
        break
    segment = segment * window
    spec = np.fft.rfft(segment)
    power = (np.abs(spec)**2) / (fs * win_power)
    Sxx[:, i] = power

# ===============================
# 4. バンドごとの平均パワー算出
# ===============================
def band_power(fmin, fmax=None):
    """指定された周波数範囲の平均パワーを返す（fmaxがNoneなら上限なし）"""
    if fmax is None:
        mask = (freqs >= fmin)
    else:
        mask = (freqs >= fmin) & (freqs < fmax)
    return Sxx[mask, :].mean(axis=0)

band_powers = {}
if show_delta:
    band_powers["δ (0–4 Hz)"] = band_power(0, 4)
if show_theta:
    band_powers["θ (4–8 Hz)"] = band_power(4, 8)
if show_alpha:
    band_powers["α (8–13 Hz)"] = band_power(8, 13)
if show_beta:
    band_powers["β (13–36 Hz)"] = band_power(13, 36)
if show_gamma:
    band_powers["γ (≥36 Hz)"] = band_power(36, None)

# ===============================
# 5. 正規化と平滑化
# ===============================
smoothed = {}
window_len = 5  # 平滑化の窓（移動平均）
for name, data in band_powers.items():
    db = 10 * np.log10(data + 1e-12)  # dBスケーリング
    smooth = np.convolve(db, np.ones(window_len)/window_len, mode='same')
    smoothed[name] = smooth

# ===============================
# 6. グラフ描画
# ===============================
plt.figure(figsize=(12, 6))

for name, data in smoothed.items():
    plt.plot(times[:len(data)], data, label=name)

plt.title("EEG Band Power Over Time (DFT-based)")
plt.xlabel("Time [s]")
plt.ylabel("Power [dB]")
plt.legend()
plt.grid(True, alpha=0.3)
plt.tight_layout()
plt.savefig("band_power_plot.png", dpi=200)
plt.show()

print("✅ 各周波数帯のパワー変化を保存しました → band_power_plot.png")
