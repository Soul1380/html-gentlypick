'use strict';
// Curated source metadata. A model appears here only as a selectable download,
// never as evidence that it is installed or usable.
const modelScope='https://modelscope.cn/models/bartowski/DeepSeek-R1-Distill-Llama-8B-GGUF';
const deepseek=['IQ2_M','IQ3_M','IQ3_XS','IQ4_NL','IQ4_XS','Q2_K','Q2_K_L','Q3_K_L','Q3_K_M','Q3_K_S','Q3_K_XL','Q4_0','Q4_1','Q4_K_L','Q4_K_M','Q4_K_S','Q5_K_L','Q5_K_M','Q5_K_S','Q6_K','Q6_K_L','Q8_0'].map(quant=>({id:`deepseek-r1-llama-8b-${quant.toLowerCase()}`,name:`DeepSeek R1 Llama 8B ${quant}`,quant,file:`DeepSeek-R1-Distill-Llama-8B-${quant}.gguf`,source:modelScope}));
const whisper=['tiny','base','small','medium','large-v3','large-v3-turbo'].map(name=>({id:`whisper-${name}`,name:`Whisper ${name}`,model:name,source:name==='large-v3-turbo'?'https://huggingface.co/mobiuslabsgmbh/faster-whisper-large-v3-turbo':`https://huggingface.co/Systran/faster-whisper-${name}`}));
module.exports={deepseek,whisper};
