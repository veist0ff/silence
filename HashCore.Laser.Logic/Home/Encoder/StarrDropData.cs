using HashCore.Laser.Logic.Command;
using HashCore.Laser.Logic.Data;
using HashCore.Laser.Logic.Home;
using HashCore.Laser.Logic.Home.Gatcha;
using HashCore.Laser.Logic.Message.Home;
using HashCore.Laser.Server;

public  class StarrDropData
{
    public int Type;

    public  void GenerateRareRolle(HomeMode homeMode)
    {

        switch (new Random().Next(0,100))
        {
            case >80:
                Type = 29;
                break;
            case > 54:
        Type = 28;
                break;
            case > 34:
        Type = 27;
                break;
            default:
                Type = 26;
                break;
        }
      
    }

    public  void GenerateRarePidarok(HomeMode homeMode)
    {

      switch (new Random().Next(0, 100))
{
    case >= 98: // 2% шанс (98-99)
        Type = 25;
        break;
    case >= 88: // 10% шанс (88-97)
        Type = 24;
        break;
    case >= 64: // 24% шанс (64-87)
        Type = 23;
        break;
    case >= 30: // 34% шанс (30-63)
        Type = 22;
        break;
    default: // 45% шанс (0-29)
        Type = 21;
        break;
}
      
    }
public static int GetRandomThumbnail()
{
    // Получаем таблицу спреев
    var sprayTable = DataTables.Get(DataType.Spray);
    if (sprayTable == null) return -1; // Если таблица не найдена, выходим

    List<int> globalIds = new List<int>(); // Список для хранения подходящих GlobalId

    // Проходим по всем данным таблицы спреев
    foreach (SprayData spray in sprayTable.GetDatas())
    {
        // Проверяем, принадлежит ли спрей к коллекции "FEUDALJAPAN"
        if (spray.ExtraCatalogCampaign == "FEUDALJAPAN")
        {
            // Получаем GlobalId спрея
            int gid = spray.GetGlobalId(); // Или используй spray.GlobalId, если это свойство

            // Добавляем GlobalId в список, если он больше 0
            if (gid > 0)
            {
                globalIds.Add(gid);
            }
        }
    }

    // Если список пуст, возвращаем -1 (ничего не найдено)
    if (globalIds.Count == 0) return -1;

    // Выбираем случайный GlobalId из списка
    Random random = new Random();
    return globalIds[random.Next(globalIds.Count)];
}
    public void GenerateDrop(HomeMode homeMode)
    {    
        LogicGiveDeliveryItemsCommand command123 = new LogicGiveDeliveryItemsCommand();
        DeliveryUnit unit = new DeliveryUnit(100);

        switch (Type)
        {
            case 29:
            {
              for(int a = 0; a< 8;a++)
                {
                switch(new Random().Next(0, 100))
                {
                    case >80:
                    {
                        GatchaDrop drop3 = new GatchaDrop(7);
                        drop3 .Count = new Random().Next(0, 10);
                        unit.AddDrop( drop3 );
                            command123.DeliveryUnits.Add(unit);
                command123.Execute(homeMode);
                 
                AvailableServerCommandMessage message = new AvailableServerCommandMessage();
                message.Command = command123;
                homeMode.GameListener.SendMessage(message);
                        break; 
                    }
                    case >60:
                    {
                        GatchaDrop drop3 = new GatchaDrop(22);
                        drop3 .Count = new Random().Next(0, 20);
                        unit.AddDrop( drop3 );
                            command123.DeliveryUnits.Add(unit);
                command123.Execute(homeMode);
                 
                AvailableServerCommandMessage message = new AvailableServerCommandMessage();
                message.Command = command123;
                homeMode.GameListener.SendMessage(message);
                        break; 
                    }
                    case >40:
                    {
                        GatchaDrop drop3 = new GatchaDrop(23);
                        drop3 .Count = new Random().Next(0, 10);
                        unit.AddDrop( drop3 );
                            command123.DeliveryUnits.Add(unit);
                command123.Execute(homeMode);
                 
                AvailableServerCommandMessage message = new AvailableServerCommandMessage();
                message.Command = command123;
                homeMode.GameListener.SendMessage(message);
                        break; 
                    }
                }
                }
                break;
            }
            case 28:
            {
                 for(int a = 0; a< 4;a++)
                {
                    switch(new Random().Next(0, 100))
                    {
                        case >80:
                            {
                        GatchaDrop drop3 = new GatchaDrop(7);
                        drop3 .Count = new Random().Next(1, 10);
                        unit.AddDrop( drop3 );
                            command123.DeliveryUnits.Add(unit);
                command123.Execute(homeMode);
                 
                AvailableServerCommandMessage message = new AvailableServerCommandMessage();
                message.Command = command123;
                homeMode.GameListener.SendMessage(message);
                        break; 
                    }
                    case >60:
                    {
                        GatchaDrop drop3 = new GatchaDrop(22);
                        drop3 .Count = new Random().Next(1, 20);
                        unit.AddDrop( drop3 );
                            command123.DeliveryUnits.Add(unit);
                command123.Execute(homeMode);
                 
                AvailableServerCommandMessage message = new AvailableServerCommandMessage();
                message.Command = command123;
                homeMode.GameListener.SendMessage(message);
                        break; 
                    }
                    case >30:
                    {
                        GatchaDrop drop3 = new GatchaDrop(23);
                        drop3 .Count = new Random().Next(1, 10);
                        unit.AddDrop( drop3 );
                            command123.DeliveryUnits.Add(unit);
                command123.Execute(homeMode);
                 
                AvailableServerCommandMessage message = new AvailableServerCommandMessage();
                message.Command = command123;
                homeMode.GameListener.SendMessage(message);
                        break; 
                    }
                   
                }
                }
                break;
             
            }
            case 27:
            {
                for(int a = 0; a< 2;a++)
                {
                        
                    switch(new Random().Next(0, 100))
                    {
                        case >80:
                        {
                            GatchaDrop drop3 = new GatchaDrop(7);
                            drop3 .Count = new Random().Next(50, 120);
                            unit.AddDrop( drop3 );
                                command123.DeliveryUnits.Add(unit);
                command123.Execute(homeMode);
                 
                AvailableServerCommandMessage message = new AvailableServerCommandMessage();
                message.Command = command123;
                homeMode.GameListener.SendMessage(message);
                            break; 
                        }
                        case >60:
                        {
                        GatchaDrop drop3 = new GatchaDrop(22);
                        drop3 .Count = new Random().Next(30, 80);
                        unit.AddDrop( drop3 );
                            command123.DeliveryUnits.Add(unit);
                command123.Execute(homeMode);
                 
                AvailableServerCommandMessage message = new AvailableServerCommandMessage();
                message.Command = command123;
                homeMode.GameListener.SendMessage(message);
                        break; 
                    }
                    case >40:
                    {
                        GatchaDrop drop3 = new GatchaDrop(24);
                        drop3 .Count = new Random().Next(40, 70);
                        unit.AddDrop( drop3 );
                            command123.DeliveryUnits.Add(unit);
                command123.Execute(homeMode);
                 
                AvailableServerCommandMessage message = new AvailableServerCommandMessage();
                message.Command = command123;
                homeMode.GameListener.SendMessage(message);
                        break; 
                    }
                     case >20:
                    {          
                        GatchaDrop reward = new GatchaDrop(11);
                        reward.DataGlobalId = GetRandomThumbnail();;
                        reward.Count = 1;
                           command123.DeliveryUnits.Add(unit);
                command123.Execute(homeMode);
                 
                AvailableServerCommandMessage message = new AvailableServerCommandMessage();
                message.Command = command123;
                homeMode.GameListener.SendMessage(message);
                        break; 
                    }
                    
                }
                }
                break;
       
            }
            case 26:
            {
                switch(new Random().Next(0, 100))
                {
                    case >80:
                    {
                        GatchaDrop drop3 = new GatchaDrop(7);
                        drop3 .Count = new Random().Next(5, 10);
                        unit.AddDrop( drop3 );
                            command123.DeliveryUnits.Add(unit);
                command123.Execute(homeMode);
                 
                AvailableServerCommandMessage message = new AvailableServerCommandMessage();
                message.Command = command123;
                homeMode.GameListener.SendMessage(message);
                        break; 
                    }
                    case >60:
                    {
                        GatchaDrop drop3 = new GatchaDrop(22);
                        drop3 .Count = new Random().Next(5, 20);
                        unit.AddDrop( drop3 );
                            command123.DeliveryUnits.Add(unit);
                command123.Execute(homeMode);
                 
                AvailableServerCommandMessage message = new AvailableServerCommandMessage();
                message.Command = command123;
                homeMode.GameListener.SendMessage(message);
                        break; 
                    }
                    case >40:
                    {
                        
                        GatchaDrop drop3 = new GatchaDrop(24);
                        drop3 .Count = new Random().Next(5, 10);
                        unit.AddDrop( drop3 );
                            command123.DeliveryUnits.Add(unit);
                command123.Execute(homeMode);
                 
                AvailableServerCommandMessage message = new AvailableServerCommandMessage();
                message.Command = command123;
                homeMode.GameListener.SendMessage(message);
                        break; 
                    }
                    case >20:
                    {
                        GatchaDrop drop3 = new GatchaDrop(25);
                        drop3 .Count = new Random().Next(10, 30);
                        unit.AddDrop( drop3 );
                            command123.DeliveryUnits.Add(unit);
                command123.Execute(homeMode);
                 
                AvailableServerCommandMessage message = new AvailableServerCommandMessage();
                message.Command = command123;
                homeMode.GameListener.SendMessage(message);
                        break; 
                    }
                  
                }
                break;
            
            }
         
        }

    }
    
    public static  void encode(ByteStream stream)
    {
        stream.WriteVInt(1);
        {
            stream.WriteDataReference(80, 13); // 80 - random_reward_containers.csv
            stream.WriteVInt(1);
            stream.WriteVInt(1);
        }
        if (5 == 3) 
        {
            stream.WriteVInt(1);
            {
                stream.WriteBoolean(true);
                stream.WriteDataReference(80, 13);
            }

            stream.WriteVInt(1); 
            {
                stream.WriteVInt(1);
                stream.WriteVInt(1);
                stream.WriteDataReference(0);
                stream.WriteVInt(0);
            }

            stream.WriteVInt(2); // ren type (1 - normal, 2 - rang ren)
            stream.WriteVInt(0); // upgrade taps (не дружит с контейнерами которые не стардропы)
        }
        else
        {
            stream.WriteVInt(0); // контейнер дата null
        }
        stream.WriteVInt(0);
        stream.WriteVInt(-1435281534);
        stream.WriteVInt(0);
        stream.WriteVInt(0);
        stream.WriteVInt(1);
        stream.WriteVInt(0);
        stream.WriteVInt(0);
        stream.WriteBoolean(false);
      
        stream.WriteVInt(0);
        {
          
        }

        Logger.Print("[Conteiners::encode] complete");
    }
}
