using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Net.Http.Headers;
using System.Text;
using Web3_Afternoon.Dtos;

namespace Web3_Afternoon.Formatters
{
    public class CarVCardInputFormatter : TextInputFormatter
    {
        public CarVCardInputFormatter()
        {
            SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("text/vcard"));

            SupportedEncodings.Add(Encoding.UTF8);
            SupportedEncodings.Add(Encoding.Unicode);
        }

        protected override bool CanReadType(Type type)
        {
            return typeof(CarAddDto).IsAssignableFrom(type);
        }

        public override async Task<InputFormatterResult> ReadRequestBodyAsync(
            InputFormatterContext context, Encoding encoding)
        {
            using var reader = new StreamReader(context.HttpContext.Request.Body, encoding);
            var body = await reader.ReadToEndAsync();

            var car = ParseVCard(body);

            if (car == null)
                return await InputFormatterResult.FailureAsync();

            return await InputFormatterResult.SuccessAsync(car);
        }

        private static CarAddDto? ParseVCard(string body)
        {
            var lines = body
                .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(l => l.Trim())
                .Where(l => !string.IsNullOrWhiteSpace(l))
                .ToList();

            if (!lines.Any(l => l.Equals("BEGIN:VCARD", StringComparison.OrdinalIgnoreCase)))
                return null;

            var dto = new CarAddDto();

            foreach (var line in lines)
            {
                if (line.StartsWith("MV:", StringComparison.OrdinalIgnoreCase))
                {
                    var value = line.Substring(3).Trim();
                    var parts = value.Split('-', 2, StringSplitOptions.TrimEntries);

                    dto.Model = parts.Length > 0 ? parts[0] : null;
                    dto.Vendor = parts.Length > 1 ? parts[1] : null;
                }
                else if (line.StartsWith("NOTE:", StringComparison.OrdinalIgnoreCase))
                {
                    var note = line.Substring(5).Trim();
                    var noteParts = note.Split('=', 2, StringSplitOptions.TrimEntries);

                    if (noteParts.Length != 2)
                        continue;

                    var key = noteParts[0];
                    var value = noteParts[1];

                    if (key.Equals("Year", StringComparison.OrdinalIgnoreCase)
                        && int.TryParse(value, out var year))
                    {
                        dto.Year = year;
                    }
                    else if (key.Equals("Engine", StringComparison.OrdinalIgnoreCase)
                        && double.TryParse(value, System.Globalization.CultureInfo.InvariantCulture, out var engine))
                    {
                        dto.Engine = engine;
                    }
                }
            }

            if (string.IsNullOrWhiteSpace(dto.Model) || string.IsNullOrWhiteSpace(dto.Vendor))
                return null;

            return dto;
        }
    }
}