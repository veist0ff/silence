# Бриф для сессии на ноуте: доразбор боёв v64 по клиенту

Этот файл пишется на VDS (`FidBomVds.play2go.cloud`, `/root/v64`). Сессия там клиента v64
не видит, поэтому работа переезжает на ноут, где лежат IDA и IDB.

## Цель

Добить сериализацию боя в сервере под реальный формат клиента v64: сейчас часть полей
в vision-стриме пишется константами-заглушками, разложение по битам угадано не везде.

## Состояние сурса (замерено на VDS 2026-09-16)

Доля write-вызовов, пишущих константу-заглушку (`WriteBoolean(false)`, `WritePositiveIntMax1(0) //v64`):

| функция | файл | write | заглушек |
|---|---|---|---|
| `GameObjectManager.Encode` | `HashCore.Laser.Logic/Battle/Objects/GameObjectManager.cs:215` | 97 | 55 (57%) |
| `Character.Encode` | `HashCore.Laser.Logic/Battle/Objects/Character.cs:6419` | 144 | 68 (47%) |
| `AreaEffect.Encode` | `HashCore.Laser.Logic/Battle/Objects/AreaEffect.cs:1093` | 6 | 3 |
| `Item.Encode` | `HashCore.Laser.Logic/Battle/Objects/Item.cs:191` | 10 | 3 |
| `Projectile.Encode` | `HashCore.Laser.Logic/Battle/Objects/Projectile.cs:3179` | 17 | 0 |

Ещё не разобраны: `ClientInput.Decode` (типы 11/15/16 читаются вслепую, 12 и 18+ не
обработаны вовсе), `BattleEndMessage` (9 мест с unknown/TODO).

## Якоря в клиенте, уже зафиксированные в комментариях сурса

Смешаны два дампа — iOS (база `0x100000000`) и android/arm64 (смещения без базы):

- `0x10046F3A0` — `LogicGameObjectManagerServer::encode` (GameObjectManager.cs:423)
- `0x1004B6C64` — StartLoadingMessage (StartLoadingMessage.cs:42)
- `sub_845298` — вложенный блок в цикле по игрокам (GameObjectManager.cs:346)
- `sub_8738BC`, `sub_8DF30C`, `sub_8FF244`, `sub_879620`, `sub_89B8B0`, `sub_877F90` — BattleEnd
- `sub_1ECCEC`, `sub_1EF8B8`, `sub_20A388`, `sub_4FB844`, `sub_518AB8`, `sub_4771CC`,
  `sub_3A5150`, `sub_5388E0`, `sub_8839E8` — Character
- `sub_430204` — Projectile, `sub_9A30AC` — BattlePlayer

## Метод

Из tutor64.md, коротко: `getMessageType` компилируется в `movz w0, #id ; ret`; адрес
геттера лежит в vtable сообщения слотом 5 (`0x28`); рядом слот 2 — `encode`, слот 3 —
`decode`. Дальше читаем decode клиента и повторяем порядок чтения в серверном encode.

Скрипт `vt.py` (рядом с этим файлом) делает шаги 1-3 автоматически, на стрипнутом
бинаре с зашифрованными строками тоже:

```
python3 vt.py libg.so --list            # все id, похожие на сообщения
python3 vt.py libg.so 24109 10555       # vtable + адреса encode/decode
python3 vt.py libg.so 24109 --disasm    # то же + дизасм через r2
```

Проверен на `/root/SilenceBrawl/libg.so`: для 24109 нашёл vtable `0xa36208`,
encode `0x305210`, decode `0x513090`; для 10555 — vtable `0xa43938`, decode `0x204864`.

## Важное предупреждение про libg.so на VDS

`/root/SilenceBrawl/libg.so` — **не клиент v64**, сверяться по нему нельзя. Доказательство:
id сообщений клиент->сервер в v64 скрамблены, и ни одного из них в этом бинаре нет,
зато есть старые (в скобках — что использует v64):

| сообщение | старый id (есть в libg.so) | v64 |
|---|---|---|
| MatchmakeRequest | 14103 | 18977 |
| GoHome | 14101 | 14456 |
| GetPlayerProfile | 14113 | 15081 |
| TeamCreate | 14350 | 12541 |
| TeamChat | 14359 | 14049 |

## Что нужно на ноуте

- IDB клиента v64 (или дамп: PADumper + Fix ELF, строки в сторовом .so зашифрованы);
- `radare2` — если работать скриптом, а не руками в IDA;
- копия сурса `v64` (на VDS это `/root/v64`, там же `v64.zip`/`v64z2.zip` в `/root`).

## С чего начинать

1. `VisionUpdateMessage` (сервер шлёт id **24109**) — decode клиента, внешняя обёртка:
   проверить поля после `Tick`/`HandledInputs`, в сурсе три булевых заглушки
   (`VisionUpdateMessage.cs:26-28`).
2. `LogicGameObjectManagerClient::decode` — цикл по игрокам, те самые 55 заглушек.
3. `Character` decode — 68 заглушек: баффы, гиперзаряд, состояния, гаджеты.
4. `ClientInputMessage` (10555) decode — добрать типы инпутов.
