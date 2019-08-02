namespace RavenBOT.ELO.Modules.Models
{
    public class Rank
    {
        public ulong RoleId { get; set; }
        public int Points { get; set; }

        public int? WinModifier { get; set; }
        public int? LossModifier { get
        {
            return LossModifier;
        } set
        {
            if (value < 0)
            {
                value = -value;
            }

            LossModifier = value;
        } }
    }
}