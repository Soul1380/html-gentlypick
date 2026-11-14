import os, sys
from faster_whisper.utils import download_model
name, root = sys.argv[1:3]
if name not in {'tiny', 'base', 'small', 'medium', 'large-v3', 'large-v3-turbo'}:
    raise ValueError('unknown model')
path = download_model(name, cache_dir=os.path.join(root, 'models'))
for required in ('model.bin','config.json','tokenizer.json'):
    if not os.path.isfile(os.path.join(path, required)):
        raise RuntimeError('missing model file: '+required)
print('Model downloaded and files present', flush=True)
