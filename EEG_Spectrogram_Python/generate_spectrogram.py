import numpy as np
import pandas as pd
import matplotlib.pyplot as plt

# ===============================
# 1. パラメータ設定
# ===============================
fs = 1000
nperseg = 1024
noverlap = 512
window = np.hamming(nperseg)
fmax = 40

# ===============================
# 2. データ読み込み
# ===============================
df = pd.read_csv("signal_data.csv")
signal = df["Signal Raw [uV]"].values
N = len(signal)

# ===============================
# 3. DFTによるスペクトログラム
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

Sxx_dB = 10 * np.log10(Sxx + 1e-12)

# ===============================
# 4. 外れ値を無視したスケーリング
# ===============================
q25, q75 = np.percentile(Sxx_dB, [25, 75])
iqr = q75 - q25
vmin = q25 - 1.5 * iqr
vmax = q75 + 1.5 * iqr
vmax = min(vmax, 5)   # 極端なスパイクを防ぐ

print(f"Auto color scale: vmin={vmin:.1f} dB, vmax={vmax:.1f} dB")

# ===============================
# 5. 描画
# ===============================
plt.figure(figsize=(12, 6))
plt.pcolormesh(times, freqs, Sxx_dB, shading='gouraud', cmap='jet', vmin=vmin, vmax=vmax)
plt.xlabel("Time [s]")
plt.ylabel("Frequency [Hz]")
plt.ylim(0, fmax)
plt.colorbar(label="Power [dB]")
plt.tight_layout()
plt.savefig("spectrogram_dft_autoscale.png", dpi=200)
plt.show()

print("✅ 自動スケーリング版スペクトログラムを保存しました → spectrogram_dft_autoscale.png")
