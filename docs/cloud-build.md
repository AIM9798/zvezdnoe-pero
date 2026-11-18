# Облачная сборка APK (без Unity на компьютере)

APK собирается на серверах GitHub (GitHub Actions) после каждой правки в `Assets/`, `Packages/` или `ProjectSettings/`. Для публичного репозитория это бесплатно.

## Один раз: ключи Unity

Unity Personal бесплатна, но сборщику нужны твоя лицензия и вход в аккаунт Unity. Ключи вводишь только ты сам на сайте GitHub, в чат их не пиши.

1. **Получи файл лицензии.**
   1. Открой Unity Hub и войди в свой аккаунт Unity.
   2. Открой шестерёнку (Preferences) → Licenses → Add → Get a free personal license.
   3. Появится файл `C:\ProgramData\Unity\Unity_lic.ulf`. Открой его Блокнотом и скопируй **всё** содержимое.
2. **Добавь секреты в GitHub.**
   1. Открой репозиторий: Settings → Secrets and variables → Actions → New repository secret.
   2. Создай три секрета:

   | Имя | Что вставить |
   |---|---|
   | `UNITY_LICENSE` | всё содержимое `Unity_lic.ulf` |
   | `UNITY_EMAIL` | почта аккаунта Unity |
   | `UNITY_PASSWORD` | пароль аккаунта Unity |

3. Открой Actions → Android APK → Run workflow, чтобы запустить первую сборку.

## Где взять APK

1. Открой Actions → последний зелёный запуск «Android APK».
2. Внизу, в разделе Artifacts, скачай `zvezdnoe-pero-apk-N`. Это zip, внутри лежит `.apk`.
3. Перекинь APK на телефон и установи. Телефон спросит разрешение на установку из неизвестных источников, разреши.

Первая сборка идёт 30–60 минут, следующие быстрее (около 15–25 минут), потому что Library кэшируется.
