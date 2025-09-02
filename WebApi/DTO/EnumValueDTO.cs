namespace Finance.Tracking.DTO;

public class EnumValueDto
{
    public string Name { get; set; } = string.Empty;  // enum name as string
    public int Value { get; set; }                    // underlying int
}
