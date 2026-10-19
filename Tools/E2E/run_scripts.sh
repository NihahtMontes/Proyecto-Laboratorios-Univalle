#!/usr/bin/env bash
# Uso: run_scripts.sh <carpeta_salida> script1.py script2.py ...
# Corre los scripts de Tools/E2E de a uno contra la copia QA y deja la salida completa en <carpeta_salida>/<script>.txt.
# Imprime solo la línea resumen de cada script.
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
OUT="$1"; shift
mkdir -p "$OUT"
export E2E_PW="$(cat "$TEMP/qa_pw.txt")" E2E_USER=qa_super E2E_DB=DB_Laboratorios_Univalle_QA_E2E PYTHONIOENCODING=utf-8
cd "$ROOT/Tools/E2E"
for s in "$@"; do
  name="$(basename "$s" .py)"
  start=$(date +%s)
  python "$s" > "$OUT/$name.txt" 2>&1
  rc=$?
  summary="$(grep -E '^== [0-9]+ comprobaciones' "$OUT/$name.txt" | tail -1)"
  echo "$name rc=$rc $(( $(date +%s) - start ))s ${summary:-SIN RESUMEN}"
done
