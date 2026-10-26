using System.Threading.Tasks;

namespace HashCore.Laser.Logic.Battle
{
    using HashCore.Laser.Logic.Avatar;
    using HashCore.Laser.Logic.Battle.Component;
    using HashCore.Laser.Logic.Battle.Objects;
    using HashCore.Laser.Logic.Battle.Input;
    using HashCore.Laser.Logic.Battle.Level;
    using HashCore.Laser.Logic.Battle.Level.Factory;
    using HashCore.Laser.Logic.Battle.Structures;
    using HashCore.Laser.Logic.Data;
    using HashCore.Laser.Logic.Data.Helper;
    using HashCore.Laser.Logic.Home;
    using HashCore.Laser.Logic.Home.Structures;
    using HashCore.Laser.Logic.Listener;
    using HashCore.Laser.Logic.Message.Battle;

    using HashCore.Laser.Logic.Util;
 
    using HashCore.Laser.Titan.Math;
    using System;
    using System.Threading;
    using System.Security.Cryptography.X509Certificates;
    using System.Numerics;
    using HashCore.Laser.Titan.Json;
    using System.Security.Cryptography;
    using HashCore.Laser.Logic.Command.Home;
    using HashCore.Laser.Logic.Message.Home;
    using HashCore.Laser.Logic.Notification;
    using HashCore.Laser.Logic.Message.Account;
    using System.ComponentModel;
    using System.Reflection.Metadata;

    public class StoryMode
    {
        public BattleMode BattleMode;
        public GameObjectManager GameObjectManager
        {
            get
            {
                return BattleMode.GetGameObjectManager();
            }
        }
        public bool DoorTest;
        public BattlePlayer Player;
        public List<string> Texts = new List<string> { "你好。", "怎么把506战神也弄来了？", "y", "e", "t" };
        public int State;
        public AreaEffect MovementHint;
        public bool WaitingMovementHint;
        public int StartWaitingTick;
        public bool Intangible;
        public int DisplayedChoices;
        public bool Choosing;
        public int CurrentChoiceStart;
        public int StartChoosingTick;
        public bool WaitSkipped;
        public int Choice;
        public Character Boss;
        public Character PlayerCharacter
        {
            get
            {
                return (Character)BattleMode.GetGameObjectManager().GetGameObjectByID(Player.OwnObjectId);
            }
        }
        public Character Shei
        {
            get
            {
                return (Character)BattleMode.GetGameObjectManager().GetGameObjectByID(BattleMode.m_players[1].OwnObjectId);
            }
        }
        public StoryMode(BattleMode battleMode)
        {
            BattleMode = battleMode;
        }
        public void TickMovementHint()
        {
            if (MovementHint == null || !WaitingMovementHint) return;
            if (PlayerCharacter.GetPosition().GetDistance(MovementHint.GetPosition()) <= 600)
            {
                GameObjectManager.RemoveGameObject(MovementHint);
                WaitingMovementHint = false;
                State++;
            }
        }
        public void Tick()
        {
            if (BattleMode.GetTicksGone() < 200) return;
            if (BattleMode.GetTicksGone() == 200)
            {
                FindObject("艾").SetPosition(0, 0, 0);
                FindObject("Null").SetPosition(0, 0, 0);
                Shei.SetPosition(8900, 12000, 0);
                Shei.SetStartRotation(90);
            }
            TickMovementHint();
            TickWait();
            TickChoice();
            switch (State)
            {
                case 0:
                    foreach (string Text in Texts)
                    {
                        FindObject(Text).SetPosition(0, 0, 0);
                    }
                    N("移动到指示地点。");
                    ShowMovementHint(8900, 15300);
                    State++;
                    break;
                case 2:
                    ShowMovementHint(8900, 13800);
                    State++;
                    break;
                case 4:
                    State++;
                    State++;
                    break;
                case 6:
                    Wait(20);
                    State++;
                    break;
                case 8:
                    FocusCameraOn(Shei);
                    State++;
                    Wait(20);
                    break;
                case 10:
                    BattleMode.m_players[1].UsePin(2, BattleMode.GetTicksGone());
                    N("我以为要等很长时间的。\n要知道这里最近已经没什么人了", "FFD700");
                    State++;
                    Wait(60);
                    break;
                case 12:
                    BattleMode.m_players[1].UsePin(4, BattleMode.GetTicksGone());
                    N("你是来当演示新模式的临时工。", "FFD700");
                    State++;
                    Wait(70);
                    break;
                case 14:
                    N("三个人齐了，\n看你后面。", "FFD700");
                    State++;
                    Wait(20);
                    break;
                case 16:
                    PlayerCharacter.SetStartRotation(90);
                    State++;
                    FocusCameraOn(8900, 13800);
                    Wait(20);
                    break;
                case 18:
                    FindObject("艾").SetPosition(9500, 17100, 0);
                    FindObject("艾").MoveTo(9500, 13800);
                    State++;
                    Wait(100);
                    break;
                case 20:
                    FindObject("艾").SetStartRotation(180);
                    State++;
                    Wait(15);
                    break;
                case 22:
                    FindObject("艾").Watch(Shei);
                    State++;
                    Wait(15);
                    break;
                case 24:
                    RevertCamera();
                    N("选择你的措辞。");
                    DisplayChoicesInt(2);
                    State++;
                    break;
                case 26:
                    Wait(20);
                    State++;
                    break;
                case 28:
                    FocusCameraOn(FindObject("艾"));
                    PlayerCharacter.Watch(FindObject("艾"));
                    N("你好。", "3CB371");
                    Wait(20);
                    State++;
                    break;
                case 30:
                    FindObject("艾").Watch(PlayerCharacter);
                    Wait(20);
                    State++;
                    break;
                case 32:
                    BattleMode.m_players[2].UsePin(2, BattleMode.GetTicksGone());
                    N("我记得很久以前碰到过你，<N>。\n当时没想到你还会回来。", "800080");
                    Wait(60);
                    State++;
                    break;
                case 34:
                    FocusCameraOn(Shei);
                    Shei.ActivateSkill(0, FindObject("艾").GetX(), FindObject("艾").GetY());
                    N("我很好奇你最近在干什么？", "FFD700");
                    Wait(40);
                    State++;
                    break;
                case 36:
                    FocusCameraOn(FindObject("艾"));
                    N("......", "800080");
                    Wait(50);
                    State++;
                    break;
                case 38:
                    //BattleMode.m_players[2].UsePin(5, BattleMode.GetTicksGone());
                    N("重建之后，礼品店就用不着员工了，", "800080");
                    Wait(50);
                    State++;
                    break;
                case 40:
                    N("那里已经近半年没有顾客了。", "800080");
                    Wait(50);
                    State++;
                    break;
                case 42:
                    BattleMode.m_players[2].UsePin(3, BattleMode.GetTicksGone());
                    N("<c800080>不过往好处想，不用从早到晚卖那堆</c><cFF0000>瓒呯骇瀹濈</c><c800080>了。</c>");
                    Wait(50);
                    State++;
                    break;
                case 44:
                    BattleMode.m_players[1].UsePin(5, BattleMode.GetTicksGone());
                    N("闲话少说，开始演示吧。", "FFD700");
                    Wait(40);
                    State++;
                    break;
                case 46:
                    N("注意看 这是一个按钮", "FFD700");
                    FocusCameraOn(12900, 12300);
                    Wait(50);
                    State++;
                    break;
                case 48:
                    N("注意看 这也是一个按钮", "FFD700");
                    FocusCameraOn(8700, 9900);
                    BattleMode.m_players[2].UsePin(3, BattleMode.GetTicksGone());
                    Wait(50);
                    State++;
                    break;
                case 50:
                    FocusCameraOn(Shei);
                    N("当它被触发时，门就会打开！\n并且这个按钮触发是瞬时的，", "FFD700");
                    Wait(50);
                    State++;
                    break;
                case 52:
                    N("也就是说，松开它门又会关闭！", "FFD700");
                    Wait(30);
                    State++;
                    break;
                case 54:
                    N("当我是傻子吗？", "800080");
                    BattleMode.m_players[2].UsePin(3, BattleMode.GetTicksGone());
                    Wait(30);
                    State++;
                    break;
                case 56:
                    N("很显然这需要团队配合，\n既然你听懂了就开始吧！", "FFD700");
                    Wait(50);
                    State++;
                    break;
                case 58:
                    N("你去按吧，<N>\n我已经对这玩意不感兴趣了。", "800080");
                    FindObject("艾").ActivateSkill(0, PlayerCharacter.GetX(), PlayerCharacter.GetY());
                    Wait(50);
                    State++;
                    break;
                case 60:
                    Shei.MoveTo(8400, 11700);
                    FindObject("艾").MoveTo(9000, 11700);
                    RevertCamera();
                    State++;
                    break;
                case 61:
                    if (DoorTest)
                    {
                        Intangible = true;
                        Wait(20);
                        State++;
                    }
                    break;
                case 63:
                    FocusCameraOn(8700, 11100);
                    Shei.MoveTo(8550, 9750);
                    BattleMode.m_players[1].UsePin(5, BattleMode.GetTicksGone());
                    FindObject("艾").MoveTo(8100, 9000);
                    State++;
                    break;
                case 64:
                    State++;
                    Wait(40);
                    break;
                case 66:
                    N("干得不错，\n现在你可以过来了！", "FFD700");
                    FindObject("艾").Watch(PlayerCharacter);
                    Shei.Watch(PlayerCharacter);
                    Wait(40);
                    State++;
                    break;
                case 68:
                    RevertCamera();
                    ShowMovementHint(8700, 10500);
                    State++;
                    break;
                case 69:
                    FindObject("艾").Watch(PlayerCharacter);
                    Shei.Watch(PlayerCharacter);
                    break;
                case 70:
                    Shei.MoveTo(8550, 9150);
                    Wait(20);
                    State++;
                    break;
                case 72:
                    FindObject("艾").MoveTo(9150, 9150);
                    Wait(40);
                    State++;
                    break;
                case 74:
                    FocusCameraOn(Shei);
                    FindObject("艾").Watch(Shei);
                    Shei.Watch(PlayerCharacter);
                    BattleMode.m_players[1].UsePin(1, BattleMode.GetTicksGone());

                    N("既然这是新模式，肯定要弄点新鲜的！", "FFD700");
                    Wait(50);
                    State++;
                    break;
                case 76:
                    Boss = (Character)GOF("LootBox", 300, 6000);
                    Boss.m_hitpoints = 30000;
                    Boss.m_maxHitpoints = 30000;
                    //Boss.SetIndex(-16);
                    Shei.Watch(Boss);
                    Boss.TriggerCharge(8550, 9150, 2000, 1, 0, 0, false);
                    N("掌声有请......", "FFD700");
                    Wait(70);
                    State++;
                    break;
                case 78:
                    State++;
                    FindObject("艾").MoveTo(9150, 10250);
                    Wait(10);
                    break;
                case 80:
                    BattleMode.m_players[1].Accessory.TriggerAccessory(Shei, 0, -1000);
                    N("注意看，这是一个Boss，\n你们需要扌", "FFD700");
                    //RevertCamera();
                    State++;
                    Wait(10);
                    break;
                case 82:
                    Boss.TriggerCharge(8550, 7250, 2000, 1, 1000, 999, false);
                    State++;
                    Wait(10);
                    break;
                case 84:
                    Shei.SetInvisible(9999);
                    State++;
                    Boss.TriggerAreaEffect(DataTables.GetAreaEffectByName("WhirlwindTrail"), Boss.GetX(), Boss.GetY(), 0, 4).Damage = 0;
                    Wait(60);
                    break;
                case 86:
                    N("他把自己秀死了...", "800080");
                    BattleMode.m_players[2].UsePin(2, BattleMode.GetTicksGone());
                    State++;
                    Wait(50);
                    break;
                case 88:
                    N("<N>，不管怎么样，先把眼前的事解决了吧。", "800080");
                    State++;
                    Wait(40);
                    break;
                case 90:
                    State++;
                    FocusCameraOn(8550, 8150);
                    Wait(10);
                    break;
                case 92:
                    N("Boss:\n古辉盒", "CD853F");
                    State++;
                    FocusCameraOn(8550, 7250);
                    Wait(40);
                    break;
                case 94:
                    State++;
                    FocusCameraOn(8550, 8150);
                    Wait(10);
                    break;
                case 96:
                    RevertCamera();
                    FindObject("艾").m_isBot = 1;
                    FindObject("艾").AddConsumableShield(8000, 1000);
                    State++;
                    break;
                case 97:
                    State++;
                    break;
                case 98:
                    if (Boss.IsAlive())
                    {
                        if (!Boss.IsCharging)
                        {
                            Character target = Boss.GetClosestVisibleEnemy();
                            //var d = 500;
                            var d = Boss.GetPosition().GetDistance(target.GetPosition());
                            if (d <= 0) break;
                            var X = Boss.GetX() + (target.GetX() - Boss.GetX()) * 3000 / d;
                            var Y = Boss.GetY() + (target.GetY() - Boss.GetY()) * 3000 / d;
                            Boss.TriggerCharge(X, Y, 1500, 1, 300, 500, false, DataTables.GetAreaEffectByName("Heal"));
                        }
                        if (BattleMode.GetTicksGone() - Boss.m_lastAIAttackTick > 10)
                        {
                            Boss.m_lastAIAttackTick = BattleMode.GetTicksGone();
                            Boss.TriggerAreaEffect(DataTables.GetAreaEffectByName("WhirlwindTrail"), Boss.GetX(), Boss.GetY(), 0, 4).Damage = 300;
                        }
                    }
                    else State = 100;
                    //Boss.
                    break;
                case 99:
                    State = 98;
                    break;
                case 100:
                    Wait(20);
                    State++;
                    break;
                case 102:
                    FocusCameraOn(PlayerCharacter);
                    FindObject("艾").m_isBot = 0;
                    FindObject("艾").MoveTo(PlayerCharacter.GetX()+1000, PlayerCharacter.GetY());
                    N("打得不错！", "800080");
                    State++;
                    Wait(60);
                    break;
                case 104:
                    Shei.InvisibleTicks = 0;
                    Shei.TriggerBlink(PlayerCharacter.GetX() - 1000, PlayerCharacter.GetY(), DataTables.GetAreaEffectByName("ArcadeTeleport"), DataTables.GetAreaEffectByName("ArcadeTeleport"), 0, 0);
                    State++;
                        Wait(10);
                    break;
                case 106:
                    FindObject("艾").Watch(Shei);
                    PlayerCharacter.SetForcedAngle(114);
                    PlayerCharacter.Watch(Shei);
                    Shei.Watch(PlayerCharacter);
                    State++;
                    Wait(40);
                    break;
                case 108:
                    BattleMode.m_players[1].UsePin(5, BattleMode.GetTicksGone());
                    N("这个地图到这里就结束了，\n感谢你们的游玩。", "FFD700");
                    State++;
                    Wait(50);
                    break;
                case 110:
                    N("这真是一个很好的点子，\n不知道我为啥没早点想出来......", "FFD700");
                    State++;
                    Wait(60);
                    break;
                case 112:
                    FindObject("艾").ActivateSkill(0, Shei.GetX(), Shei.GetY());
                    N("接下来继续做就可以了啊", "800080");
                    State++;
                    Wait(60);
                    break;
                case 114:
                    BattleMode.m_players[1].UsePin(2, BattleMode.GetTicksGone());
                    N("但是我接下去\n很可能就没什么时间了...", "FFD700");
                    State++;
                    Wait(60);
                    break;
                case 116:
                    BattleMode.m_players[2].UsePin(5, BattleMode.GetTicksGone());
                    N("虽然不知道你要去干啥\n但还是希望你赶快弄完。", "800080");
                    State++;
                    Wait(60);
                    break;
                case 118:
                    Shei.ActivateSkill(0, PlayerCharacter.GetX(),PlayerCharacter.GetY());
                    N("临时工，你也看到了；\n这不会是我们最后一次见面......", "FFD700");
                    State++;
                    Wait(60);
                    break;
            }
        }
        public void TickChoice()
        {
            if (!Choosing) return;
            if (BattleMode.GetTicksGone() - StartChoosingTick == 99)
            {
                ChoiceChosen(-1);
                return;
            }
            for (int i = CurrentChoiceStart; i < DisplayedChoices; i++)
            {
                FindObject(Texts[i]).m_hitpoints = 5000 - (BattleMode.GetTicksGone() - StartChoosingTick) * 50;
            }
        }
        public void Init()
        {
            AddPlayer("Shei", 0, 0, 255);
            AddPlayer("艾德加", 43);
            AddPlayer("Null");
            foreach (string Text in Texts)
            {
                AddPlayer(Text, 187);
            }
        }
        public void DisplayChoicesInt(int Choices)
        {
            DisplayChoices(Texts.Skip(DisplayedChoices).Take(Choices).ToList());
            DisplayedChoices += Choices;
            CurrentChoiceStart = DisplayedChoices - Choices;
        }
        public void DisplayChoices(List<string> Choices)
        {
            Choosing = true;
            StartChoosingTick = BattleMode.GetTicksGone();
            for (int i = 0; i < Choices.Count(); i++)
            {
                var angle = 180 + i * 180 / (Choices.Count() - 1);
                FindObject(Choices[i]).SetPosition(PlayerCharacter.GetX() + LogicMath.GetRotatedX(1000, 0, angle), PlayerCharacter.GetY() + LogicMath.GetRotatedY(1000, 0, angle), 0);
                FindObject(Choices[i]).m_hitpoints = 5000;
                FindObject(Choices[i]).m_maxHitpoints = 5000;

            }
        }
        public void ChoiceChosen(int x, int y = -1)
        {

            int Choice = -1;
            if (x != -1)
            {
                var angle = LogicMath.GetAngle(x, y);
                var delta = 360;
                for (int i = CurrentChoiceStart; i < DisplayedChoices; i++)
                {
                    var d = LogicMath.Abs(angle - (180 + i * 180 / (DisplayedChoices - CurrentChoiceStart - 1)));
                    if (d < delta)
                    {
                        delta = d;
                        Choice = i;
                    }
                }
            }
            if (Choice != -1)
            {
                FindObject(Texts[CurrentChoiceStart + Choice]).AddShield(999, 1);
            }
            for (int i = CurrentChoiceStart; i < DisplayedChoices; i++)
            {
                //if (i == Choice) continue;
                FindObject(Texts[i]).SetInvisible(99999);
            }
            Choosing = false;
            this.Choice = Choice;
            State++;

        }
        public void AddGameObjects()
        {
            GOF("DoorHorizontal", 8700, 11100);
            GOF("ButtonHold", 12900, 12300);
            GOF("ButtonHold", 8700, 9900);
        }
        public Character FindObject(string name)
        {
            foreach (Character character in GameObjectManager.GetCharacters())
            {
                if (character.GetPlayer() == null || !character.GetPlayer().DisplayData.Name.StartsWith(name)) continue;
                return character;
            }
            return null;
        }
        public void N(string text, string color = null)
        {
            string message = text.Replace("<N>", Player.DisplayData.Name);
            if (color != null)
            {
                message = "<c" + color + ">" + message + "</c>";
            }
            Player.GameListener.SendTCPMessage(new RankedMatchTerminatedMessage() { Name = message });
        }
        public GameObject GOF(string name, int x, int y)
        {
            GameObject gameObject = null;
            AreaEffectData areaEffectData = DataTables.GetAreaEffectByName(name);
            CharacterData characterData = DataTables.GetCharacterByName(name);
            ItemData itemData = DataTables.GetItemByName(name);
            if (areaEffectData != null) gameObject = GameObjectFactory.CreateGameObjectByData(areaEffectData);
            else if (characterData != null) gameObject = GameObjectFactory.CreateGameObjectByData(characterData);
            else if (itemData != null) gameObject = GameObjectFactory.CreateGameObjectByData(itemData);
            if (gameObject != null)
            {
                gameObject.SetIndex(-16);
                gameObject.SetPosition(x, y, 0);
                BattleMode.GetGameObjectManager().AddGameObject(gameObject);
                return gameObject;
            }
            return null;
        }
        public BattlePlayer AddPlayer(string Name, int Character = 0, int Skin = 0, int accessory = 0)
        {
            BattlePlayer Shei = BattlePlayer.CreateStoryModeDummy(Name, BattleMode.m_players.Count, 0, Character, Skin, accessory);
            BattleMode.AddPlayer(Shei, -1);
            return Shei;
        }
        public void FocusCameraOn(GameObject Target)
        {
            PlayerCharacter.SetVisionOverride(Target.GetX(), Target.GetY(), 9999999);
            Intangible = true;
        }
        public void FocusCameraOn(int X, int Y)
        {
            PlayerCharacter.SetVisionOverride(X, Y, 9999999);
            Intangible = true;
        }
        public void RevertCamera()
        {
            PlayerCharacter.VisionOverrideTicks = 0;
            Intangible = false;
        }
        public void ShowMovementHint(int x, int y)
        {
            AreaEffect areaEffect = new AreaEffect(DataTables.GetAreaEffectByName("BossSpawn"));
            areaEffect.SetPosition(x, y, 0);
            GameObjectManager.AddGameObject(areaEffect);
            areaEffect.Trigger();
            areaEffect.EndingTick = 32767;
            MovementHint = areaEffect;
            WaitingMovementHint = true;
        }
        public void Wait(int Ticks)
        {
            StartWaitingTick = BattleMode.GetTicksGone() + Ticks;
        }
        public void TickWait()
        {
            if (WaitSkipped)
            {
                WaitSkipped = false;
                State++;
                return;
            }
            if (BattleMode.GetTicksGone() == StartWaitingTick) State++;
        }
    }
}
