#!/usr/bin/env bash
set -eu

APP_DESKTOP="/usr/share/applications/medproject.desktop"
ICON_PATH="/usr/share/icons/hicolor/256x256/apps/medproject.png"

if [[ ! -f "${APP_DESKTOP}" ]]; then
  echo "ОШИБКА: файл ярлыка не найден: ${APP_DESKTOP}"
  echo "Проверьте, что пакет установлен."
  exit 1
fi

DESK_DIR="$(xdg-user-dir DESKTOP 2>/dev/null || true)"
if [[ -z "${DESK_DIR}" || ! -d "${DESK_DIR}" ]]; then
  # Fallback: many Russian locales use this folder name
  if [[ -d "${HOME}/Рабочий стол" ]]; then
    DESK_DIR="${HOME}/Рабочий стол"
  elif [[ -d "${HOME}/Desktop" ]]; then
    DESK_DIR="${HOME}/Desktop"
  else
    echo "ОШИБКА: не удалось найти папку рабочего стола."
    echo "Подсказка: проверьте вывод команды: xdg-user-dir DESKTOP"
    exit 1
  fi
fi

TARGET="${DESK_DIR}/medproject.desktop"
cp -f "${APP_DESKTOP}" "${TARGET}"
chmod +x "${TARGET}" || true

# For MATE/Caja it's often more reliable to use an absolute icon path on Desktop shortcuts.
sed -i "s|^Icon=.*|Icon=${ICON_PATH}|" "${TARGET}"

echo "Created desktop shortcut: ${TARGET}"
echo "Ярлык создан: ${TARGET}"
echo "Если файловый менеджер спросит — выберите «Доверять/Разрешить запуск»."

