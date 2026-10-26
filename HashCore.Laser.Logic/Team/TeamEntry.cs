namespace HashCore.Laser.Logic.Team
{
    using System.Globalization;
    using System.Xml.Linq;
    using Microsoft.Win32.SafeHandles;

    using HashCore.Laser.Logic.Listener;
    using HashCore.Laser.Logic.Message.Team;
    using HashCore.Laser.Logic.Message.Team.Stream;
    using HashCore.Laser.Logic.Team.Stream;

    public class TeamEntry
    {
        public long Id;
        public int Type;

        public int LocationId;

        public List<TeamMember> Members { get; set; }
        public List<TeamInviteEntry> Invites { get; set; }
        public List<TeamJoinRequest> JoinRequests { get; set; }
        public List<int> DisabledBots { get; set; }
        public int EventSlot { get; set; }
        public List<int> CustomModifiers;

        private long EntryCounter;
        public List<TeamStreamEntry> Stream { get; private set; }

        public TeamEntry()
        {
            Members = new List<TeamMember>();
            Invites = new List<TeamInviteEntry>();
            JoinRequests = new List<TeamJoinRequest>();
            DisabledBots = new List<int>();
            Stream = new List<TeamStreamEntry>();
            CustomModifiers = new List<int>();

            EventSlot = 1;
            EntryCounter = 0;
        }

        public void AddStreamEntry(TeamStreamEntry entry)
        {
            if (entry == null) return;

            entry.Id = ++EntryCounter;
            Stream.Add(entry);
            TeamStreamMessage message = new TeamStreamMessage();
            message.TeamId = Id;
            message.Entries = new TeamStreamEntry[] { entry };

            foreach (var member in Members.ToArray())
            {
                if (LogicServerListener.Instance.IsPlayerOnline(member.AccountId))
                {
                    LogicServerListener.Instance.GetGameListener(member.AccountId).SendTCPMessage(message);
                }
            }
        }

        public void StreamUpdated()
        {
            
        }

        public TeamMember GetMember(long id)
        {
            return Members.Find(member => member.AccountId == id);
        }

        public TeamInviteEntry GetInviteById(long id)
        {
            return Invites.Find(invite => invite.Id == id);
        }

        public bool IsEveryoneReady()
        {
            foreach (TeamMember member in Members)
            {
                if (!member.IsReady) return false;
            }
            return true;
        }

        public void TeamUpdated()
        {
            TeamMessage message = new TeamMessage();
            message.Team = this;

            foreach (var member in Members.ToArray())
            {
                //Debugger.Print("isPlayerOnline:" + LogicServerListener.Instance.IsPlayerOnline(member.AccountId));
                if (LogicServerListener.Instance.IsPlayerOnline(member.AccountId))
                {
                    LogicServerListener.Instance.GetGameListener(member.AccountId).SendTCPMessage(message);
                }
            }
            //Debugger.Print(LocationId.ToString());
        }

        public void Encode(ByteStream Stream)
        {
            Stream.WriteVInt(Type); // Room Type
            Stream.WriteBoolean(Type == 1);
            Stream.WriteVInt(3); // Team capacity

            Stream.WriteLong(Id);
            Stream.WriteVInt(0);
            Stream.WriteBoolean(false);
            Stream.WriteBoolean(false);
            Stream.WriteVInt(0);
            Stream.WriteVInt(0);
            Stream.WriteVInt(0);

            ByteStreamHelper.WriteDataReference(Stream, LocationId);

            // ByteStreamHelper::readBattlePlayerMap - a bare flag when there is no map.
            Stream.WriteBoolean(false);

            Stream.WriteVInt(Members.Count);
            foreach (TeamMember entry in Members.ToArray())
            {
                entry.Encode(Stream);
            }

            Stream.WriteVInt(Invites.Count);
            foreach (TeamInviteEntry entry in Invites.ToArray())
            {
                entry.Encode(Stream);
            }

            TeamJoinRequest[] requests = JoinRequests.ToArray();
            Stream.WriteVInt(requests.Length);
            foreach (TeamJoinRequest request in requests)
            {
                request.Encode(Stream);
            }

            Stream.WriteVInt(DisabledBots.Count);
            foreach (int i in DisabledBots)
            {
                Stream.WriteVInt(i);
            }

            // map<int, int> the client fills with sub_100458EC0; no server-side use yet.
            Stream.WriteVInt(0);

            Stream.WriteBoolean(true);//enable chat
            Stream.WriteBoolean(true);//Show Accessory
            Stream.WriteBoolean(true);//Show Gears

            ByteStreamHelper.WriteIntList(Stream, CustomModifiers);
        }

        public void EncodeAllianceTeamEntry(ByteStream stream)
        {
            stream.WriteVInt(Type);
            stream.WriteVInt(3);//MaxPlayer
            stream.WriteLong(Id);
            stream.WriteVInt(0);//?

            // Two SocialTeamEntry::readId values - each is a VInt pair, not four ints.
            stream.WriteVLong(0);
            stream.WriteVLong(0);

            stream.WriteBoolean(false);
            stream.WriteBoolean(false);
            stream.WriteBoolean(false);

            TeamMember[] members = Members.ToArray();
            stream.WriteVInt(members.Length);
            foreach (TeamMember entry in members)
            {
                entry.EncodeSocialEntry(stream);
            }

            ByteStreamHelper.WriteDataReference(stream, LocationId);
            stream.WriteVInt(0);
            stream.WriteBoolean(false);

            // Read last into SocialTeamEntry+20; leaving it out cut the entry short.
            stream.WriteVInt(0);
        }
    }
}
