using System;
using System.Collections.Generic;
// Убираем общий using, чтобы не было конфликта
using HashCore.Laser.Logic.Command; 

namespace HashCore.Laser.Logic.Message.Home
{
    public class EndClientTurnMessage : GameMessage
    {
        public int Tick;
        public int Checksum;

        // Используем полное имя класса HashCore.Laser.Logic.Command.Command, чтобы избежать ошибки CS0118
        public List<HashCore.Laser.Logic.Command.Command> Commands;

        public EndClientTurnMessage() : base()
        {
            Commands = new List<HashCore.Laser.Logic.Command.Command>();
        }

        public override void Decode()
        {
            Stream.ReadBoolean();
            Tick = Stream.ReadVInt();
            Checksum = Stream.ReadVInt();

            int count = Stream.ReadVInt();
            
            Console.WriteLine($"[EndClientTurn] Обработка тика: {Tick}, количество команд: {count}");

            for (int i = 0; i < count; i++)
            {
                // Здесь также используем полное имя типа для переменной
                HashCore.Laser.Logic.Command.Command command = CommandManager.DecodeCommand(Stream);
                
                if (command == null)
                {
                    Console.WriteLine($"[EndClientTurn] ОШИБКА: Команда под индексом {i} не была декодирована (NULL). Цикл прерван.");
                    return;
                }

                int type = command.GetCommandType();
                Console.WriteLine($"[EndClientTurn] Успешно декодирована команда. ID: {type} (Индекс в пакете: {i})");
                
                Commands.Add(command);
            }
        }

        public override int GetMessageType()
        {
            return 14102;
        }

        public override int GetServiceNodeType()
        {
            return 9;
        }
    }
}
