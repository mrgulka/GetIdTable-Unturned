using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Text;

namespace TestProject;

public abstract class Program
{
    internal static string DirectoryPath = @"C:\Program Files (x86)\Steam\steamapps\workshop\content\304930\1664827613";
    private static string LocationToSave = AppContext.BaseDirectory;
    private static bool ConsoleLog = true;
    private static string DirectoryUnturnedIcons = @"C:\Program Files (x86)\Steam\steamapps\common\Unturned\Extras\Icons";
    private static async Task Main()
    {
        Console.WriteLine("\nWrite full path to directory mod (example: C:\\Program Files (x86)\\Steam\\steamapps\\workshop\\content\\304930\\1664827613) :");
        DirectoryPath = Console.ReadLine();
        Console.WriteLine("\nWrite location to save data (if next .exe - press 'enter' key):");
        var text1 = Console.ReadLine();
        if (!string.IsNullOrEmpty(text1))
        {
            LocationToSave = text1;
        }
        Console.WriteLine("\nWrite full path to directory Icons (ex. C:\\Program Files (x86)\\Steam\\steamapps\\common\\Unturned\\Extras\\Icons):");
        DirectoryUnturnedIcons = Console.ReadLine();
        Console.WriteLine("\nOk, lets go!\n");


        var list = new List<ItemData>();

        foreach (var file in Directory.EnumerateFiles(DirectoryPath, "*.*", SearchOption.AllDirectories))
        {
            if (!file.EndsWith(".dat") || file.EndsWith("English.dat"))
            {
                continue;
            }

            if (ConsoleLog)
            {
                Console.WriteLine($">> Parsing {file}");
            }

            var datFile = await ReadDatFile(file, false);

            if (datFile == null)
            {
                if (ConsoleLog)
                {
                    Console.WriteLine($">> Failed parsing {file} . Continue.");
                }
                continue;
            }

            var idIndex = datFile.FindIndex(x => x[0] == "ID");

            if (idIndex == -1)
            {
                continue;
            }
            var id = ushort.Parse(datFile[idIndex][1]);

            var type = datFile.Find(x => x[0] == "Type")?[1];
            var rarity = datFile.Find(x => x[0] == "Rarity")?[1];

            var datEnglishFile = await ReadEnglishFile(file);

            string? name = null;
            string? description = null;

            if (datEnglishFile != null)
            {
                name = datEnglishFile[datEnglishFile.FindIndex(x => x[0] == "Name")][1];
                var descIndex = datEnglishFile.FindIndex(x => x[0] == "Description");
                if (descIndex != -1)
                {
                    description = datEnglishFile[descIndex][1];
                }
            }

            name ??= string.Join("", file.Split('\\')[^1][..^4]);

            list.Add(new ItemData
            {
                ID = id,
                Name = name,
                Type = type,
                Rarity = rarity,
                Description = description,
            });

            if (ConsoleLog)
            {
                Console.WriteLine($">> Add item: {id} - {name}\n");
            }
        }

        list.Sort((a, b) => a.ID.CompareTo(b.ID));

        ToJson.Save(list, LocationToSave);


        //

        var locImages = LocationToSave + @"images\";
        Directory.CreateDirectory(locImages);

        var imageNames = Directory.GetFiles(DirectoryUnturnedIcons);

        foreach (var item in list)
        {
            if (item.Type == "Effect")
            {
                continue;
            }
            foreach (var imagePath in imageNames)
            {
                if (imagePath[..^4].EndsWith("_" + item.ID))
                {
                    File.Copy(imagePath, locImages + Path.GetFileName(imagePath), true);
                    Console.WriteLine($"Successful copy {imagePath} to {locImages}\n");
                }
            }
        }

    }

    private static Task<List<string[]>?> ReadDatFile(string path, bool englishFile)
    {
        List<string[]>? exitList = new();
        try
        {
            using (var reader = new StreamReader(path, Encoding.UTF8))
            {
                var builder = new StringBuilder((int)reader.BaseStream.Length + 1);
                while (!reader.EndOfStream)
                {
                    var chr = (char)reader.Read();
                    if (chr == 0)
                    {
                        chr = ' ';
                    }

                    builder.Append(chr);
                }

                var resultArray = builder.ToString().Split(["\n", "\r"], StringSplitOptions.RemoveEmptyEntries);

                foreach (var item in resultArray)
                {
                    exitList.Add(englishFile
                        ? item.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries)
                        : item.Split(' ', StringSplitOptions.RemoveEmptyEntries));
                }
            }
        }
        catch
        {
            if (ConsoleLog)
            {
                Console.WriteLine($"English.dat not found! Continue...");
            }
            exitList = null;
        }

        return Task.FromResult(exitList);
    }

    private static async Task<List<string[]>?> ReadEnglishFile(string path)
    {
        var array = path.Replace("\\", "**\\**").Split("**")[..^1];

        path = string.Join("", array);

        path += "English.dat";

        return await ReadDatFile(path, true);
    }

    public static Bitmap ResizeImage(Image image, int width, int height)
    {
        var destRect = new Rectangle(0, 0, width, height);
        var destImage = new Bitmap(width, height);

        destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

        using var graphics = Graphics.FromImage(destImage);
        graphics.CompositingMode = CompositingMode.SourceCopy;
        graphics.CompositingQuality = CompositingQuality.HighQuality;
        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
        graphics.SmoothingMode = SmoothingMode.HighQuality;
        graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

        using var wrapMode = new ImageAttributes();
        wrapMode.SetWrapMode(WrapMode.TileFlipXY);
        graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);

        return destImage;
    }
}

public record ItemData
{
    public ushort ID { get; set; }
    public string? Name { get; set; }
    public string? Type { get; set; }
    public string? Rarity { get; set; }
    public string? Description { get; set; }
}