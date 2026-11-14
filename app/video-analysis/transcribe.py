import ctypes, json, os, sys
from ctypes import wintypes

ROOT = os.environ.get('GENTLYPICK_WHISPER_HOME', os.path.join(os.environ.get('LOCALAPPDATA', os.path.expanduser('~')), 'GentlyPick', 'whisper'))
sys.path.insert(0, os.path.join(ROOT, 'vendor'))

# faster-whisper 1.2.1 still passes an argument removed by PyAV 19.
try:
    import av
except ImportError:
    print('Whisper 运行组件未安装，请先在模型管理中安装', file=sys.stderr)
    sys.exit(2)
if int(av.__version__.split('.')[0]) >= 19:
    _av_open = av.open
    def _compatible_open(*args, **kwargs):
        kwargs.pop('metadata_errors', None)
        return _av_open(*args, **kwargs)
    av.open = _compatible_open

try:
    from faster_whisper import WhisperModel
except ImportError:
    print('Whisper 运行组件未安装', file=sys.stderr)
    sys.exit(2)

audio, model_name = sys.argv[1:3]
if model_name not in {'tiny', 'base', 'small', 'medium', 'large-v3', 'large-v3-turbo'}:
    print('不支持的本地模型名称', file=sys.stderr)
    sys.exit(2)

cache = os.path.join(ROOT, 'models')
os.makedirs(cache, exist_ok=True)
print(f'正在加载 {model_name}（首次使用会下载到本项目的 local-whisper/models）', file=sys.stderr, flush=True)
try:
    model = WhisperModel(model_name, device='cpu', compute_type='int8', download_root=cache, local_files_only=True)
except Exception:
    model = WhisperModel(model_name, device='cpu', compute_type='int8', download_root=cache, local_files_only=False)
parts, info = model.transcribe(audio, beam_size=5, vad_filter=True, language=None)
try:
    from opencc import OpenCC
    chinese_converter = OpenCC('t2s')
except ImportError:
    chinese_converter = None
def simplified(text):
    if chinese_converter:
        return chinese_converter.convert(text)
    convert = ctypes.windll.kernel32.LCMapStringEx
    convert.argtypes = [wintypes.LPCWSTR, wintypes.DWORD, wintypes.LPCWSTR, ctypes.c_int, wintypes.LPWSTR, ctypes.c_int, ctypes.c_void_p, ctypes.c_void_p, ctypes.c_size_t]
    convert.restype = ctypes.c_int
    size = convert('zh-CN', 0x02000000, text, -1, None, 0, None, None, 0)
    if not size:
        raise ctypes.WinError()
    output = ctypes.create_unicode_buffer(size)
    if not convert('zh-CN', 0x02000000, text, -1, output, size, None, None, 0):
        raise ctypes.WinError()
    return output.value
segments = []
for s in parts:
    if s.text.strip():
        text = s.text.strip()
        segments.append({'start': s.start, 'end': s.end, 'text': simplified(text) if info.language == 'zh' else text})
print(json.dumps({'segments': segments, 'language': info.language}, ensure_ascii=True), flush=True)
