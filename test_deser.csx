#r "bin/Debug/net9.0-windows10.0.26100.0/win-x64/WordFormatterUI.dll"
using WordFormatterUI.Models.History;
using System.Text.Json;
var opts = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
var json = "{\"name\":\"test.docx\",\"outputName\":\"test-R.docx\",\"status\":\"success\"}";
var item = JsonSerializer.Deserialize<HistoryFileItemDto>(json, opts);
Console.WriteLine("OutputName: " + item.OutputName);
