namespace RavenBOT.ELO.Modules.Models
{
    public class Rank
    {
        public ulong RoleId { get; set; }
        public int Points { get; set; }

        public int? WinModifier { get; set; }

        private int? _LossModifier;
        public int? LossModifier { get
        {
            return _LossModifier;
        } set
        {
            if (value < 0)
            {
                value = -value;
            }

            _LossModifier = value;
        } }
    }
}