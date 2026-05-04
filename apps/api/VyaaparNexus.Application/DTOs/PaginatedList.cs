using System.Collections.Generic;

namespace VyaaparNexus.Application.DTOs;

public class PaginatedList<T>
{
    public List<T> Items { get; set; } = new();
    public int Page { get; set; }
    public int Size { get; set; }
    public int Total { get; set; }
    public int TotalPages { get; set; }
}
