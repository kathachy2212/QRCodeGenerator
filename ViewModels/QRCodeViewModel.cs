namespace QRCodeGenerator.ViewModels
{
    public class QRCodeViewModel
    {
        public int Step { get; set; } = 1;
        public int FieldCount { get; set; }
        public List<string>? FieldNames { get; set; }
        public List<string>? FieldValues { get; set; }
        public string? QRCodeImageBase64 { get; set; }
    }
}
