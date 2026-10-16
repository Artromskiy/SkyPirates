namespace SlyPirates.Shared.Core.Sheets
{
    public readonly struct Sheet
    {
        public readonly string Name;
        public readonly int HeaderRows;
        public readonly int Id;

        public Sheet(string name, int headerRows, int id)
        {
            Name = name;
            HeaderRows = headerRows;
            Id = id;
        }
    }
}
