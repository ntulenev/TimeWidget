using System.Text.Json;

using FluentAssertions;

using TimeWidget.Domain.Widget;
using TimeWidget.Infrastructure.Persistence;

namespace TimeWidget.Infrastructure.Tests;

[Collection(JsonWidgetPlacementStoreCollection.Name)]
public sealed class JsonWidgetPlacementStoreTests
{
    [Fact(DisplayName = "Save Window Placement should persist JSON payload.")]
    [Trait("Category", "Unit")]
    public void SaveWindowPlacementShouldPersistJsonPayload()
    {
        // Arrange
        ExecuteAgainstStoreFile(settingsFilePath =>
        {
            // Arrange
            var store = new JsonWidgetPlacementStore();

            // Act
            store.SaveWindowPlacement(new WidgetPlacement(10, 20, "dip"));

            // Assert
            File.Exists(settingsFilePath).Should().BeTrue();
            var savedPlacement = JsonSerializer.Deserialize<WidgetPlacement>(File.ReadAllText(settingsFilePath));
            savedPlacement.Should().Be(new WidgetPlacement(10, 20, "dip"));
        });
    }

    [Fact(DisplayName = "Try Load Window Placement should return false when settings file is missing.")]
    [Trait("Category", "Unit")]
    public void TryLoadWindowPlacementShouldReturnFalseWhenSettingsFileIsMissing()
    {
        // Arrange
        ExecuteAgainstStoreFile(settingsFilePath =>
        {
            // Arrange
            File.Exists(settingsFilePath).Should().BeFalse();
            var store = new JsonWidgetPlacementStore();

            // Act
            var loaded = store.TryLoadWindowPlacement(out var placement);

            // Assert
            loaded.Should().BeFalse();
            placement.Should().Be(default(WidgetPlacement));
        });
    }

    [Fact(DisplayName = "Try Load Window Placement should normalize blank units to pixels.")]
    [Trait("Category", "Unit")]
    public void TryLoadWindowPlacementShouldNormalizeBlankUnitsToPixels()
    {
        // Arrange
        ExecuteAgainstStoreFile(settingsFilePath =>
        {
            // Arrange
            Directory.CreateDirectory(Path.GetDirectoryName(settingsFilePath)!);
            File.WriteAllText(
                settingsFilePath,
                """{"Left":15,"Top":25,"Unit":" "}""");
            var store = new JsonWidgetPlacementStore();

            // Act
            var loaded = store.TryLoadWindowPlacement(out var placement);

            // Assert
            loaded.Should().BeTrue();
            placement.Left.Should().Be(15);
            placement.Top.Should().Be(25);
            placement.Unit.Should().Be(WidgetPlacement.PixelUnit);
        });
    }

    private static void ExecuteAgainstStoreFile(Action<string> action)
    {
        var settingsFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "TimeWidget",
            "widget-settings.json");
        var settingsDirectory = Path.GetDirectoryName(settingsFilePath)!;
        var backupPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.json");
        var hadExistingFile = File.Exists(settingsFilePath);

        Directory.CreateDirectory(settingsDirectory);

        if (hadExistingFile)
        {
            File.Copy(settingsFilePath, backupPath, overwrite: true);
        }

        try
        {
            if (File.Exists(settingsFilePath))
            {
                File.Delete(settingsFilePath);
            }

            action(settingsFilePath);
        }
        finally
        {
            if (File.Exists(settingsFilePath))
            {
                File.Delete(settingsFilePath);
            }

            if (hadExistingFile)
            {
                File.Copy(backupPath, settingsFilePath, overwrite: true);
                File.Delete(backupPath);
            }
        }
    }
}

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class JsonWidgetPlacementStoreCollection : ICollectionFixture<object>
{
    public const string Name = "JsonWidgetPlacementStore";
}

