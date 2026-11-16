import io
import json
import os
import sys
import threading
import time
from huggingface_hub import snapshot_download
from tqdm.auto import tqdm

PREFIX = 'GENTLYPICK_PROGRESS '
_lock = threading.RLock()

def report(**event):
    with _lock:
        print(PREFIX + json.dumps(event), flush=True)

class DownloadProgress(tqdm):
    """Forward actual Hub counters, distinguishing bytes from file counts."""
    def __init__(self, *args, **kwargs):
        name = kwargs.pop('name', '') or ''
        self._gp_transfer = kwargs.get('unit') == 'B' and name.endswith('.transfer')
        self._gp_bytes = kwargs.get('unit') == 'B' and not self._gp_transfer
        self._gp_files = kwargs.get('unit') != 'B'
        self._gp_time = time.monotonic()
        self._gp_last_bytes = kwargs.get('initial', 0)
        self._gp_last_emit = 0
        kwargs['disable'] = False
        kwargs['file'] = io.StringIO()
        super().__init__(*args, **kwargs)
        self._report(force=True)

    def _report(self, force=False):
        if not hasattr(self, 'n'):
            return
        now = time.monotonic()
        if not force and now - self._gp_last_emit < 0.4:
            return
        with _lock:
            self._gp_last_emit = now
            if self._gp_bytes or self._gp_transfer:
                elapsed = now - self._gp_time
                speed = max(0, self.n - self._gp_last_bytes) / elapsed if elapsed > 0 else 0
                self._gp_time, self._gp_last_bytes = now, self.n
                if self._gp_transfer:
                    report(phase='download', speed=speed)
                else:
                    report(phase='download', downloaded=max(0, int(self.n)),
                           total=int(self.total) if self.total else None)
            elif self._gp_files:
                report(phase='download', filesDone=int(self.n),
                       filesTotal=int(self.total) if self.total else None)

    def update(self, n=1):
        result = super().update(n)
        self._report()
        return result

    def refresh(self, *args, **kwargs):
        result = super().refresh(*args, **kwargs)
        self._report()
        return result

    def close(self):
        super().close()
        self._report(force=True)

def main():
    name, root = sys.argv[1:3]
    if name not in {'tiny', 'base', 'small', 'medium', 'large-v3', 'large-v3-turbo'}:
        raise ValueError('unknown model')
    repo = ('mobiuslabsgmbh/faster-whisper-large-v3-turbo' if name == 'large-v3-turbo'
            else 'Systran/faster-whisper-' + name)
    report(phase='download', downloaded=0, total=None, speed=0)
    model_path = snapshot_download(
        repo, cache_dir=os.path.join(root, 'models'),
        allow_patterns=['config.json', 'preprocessor_config.json', 'model.bin',
                        'tokenizer.json', 'vocabulary.*'],
        tqdm_class=DownloadProgress,
    )
    report(phase='verify')
    for required in ('model.bin', 'config.json', 'tokenizer.json'):
        if not os.path.isfile(os.path.join(model_path, required)):
            raise RuntimeError('missing model file: ' + required)
    report(phase='verify', filesReady=True)
    print('Model downloaded and files present', flush=True)

if __name__ == '__main__':
    main()
