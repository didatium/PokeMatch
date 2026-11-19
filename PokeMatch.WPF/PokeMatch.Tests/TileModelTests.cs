using PokeMatch.WPF.Models;
using Xunit;

namespace PokeMatch.Tests
{
    public class TileModelTests
    {
        [Fact]
        public void Constructor_StoresCoordinatesAndId()
        {
            var tile = new TileModel(x: 3, y: 5, id: 7, state: TileState.Normal);

            Assert.Equal(3, tile.X);
            Assert.Equal(5, tile.Y);
            Assert.Equal(7, tile.Id);
            Assert.Equal(TileState.Normal, tile.State);
        }

        [Fact]
        public void Constructor_DefaultsToEmptyDeletedTile()
        {
            var tile = new TileModel(0, 0);

            Assert.Equal(TileModel.EmptyId, tile.Id);
            Assert.True(tile.IsDeleted);
        }

        [Fact]
        public void Select_ThenDeselect_TogglesState()
        {
            var tile = new TileModel(0, 0, 1, TileState.Normal);

            tile.Select();
            Assert.True(tile.IsSelected);

            tile.Deselect();
            Assert.Equal(TileState.Normal, tile.State);
        }

        [Fact]
        public void Select_OnDeletedTile_DoesNothing()
        {
            var tile = new TileModel(0, 0, 1, TileState.Deleted);

            tile.Select();

            Assert.True(tile.IsDeleted);
            Assert.False(tile.IsSelected);
        }

        [Fact]
        public void Deselect_OnDeletedTile_KeepsDeleted()
        {
            var tile = new TileModel(0, 0, 1, TileState.Deleted);

            tile.Deselect();

            Assert.True(tile.IsDeleted);
        }

        [Fact]
        public void Delete_SetsDeletedState()
        {
            var tile = new TileModel(0, 0, 1, TileState.Selected);

            tile.Delete();

            Assert.True(tile.IsDeleted);
            Assert.False(tile.IsSelected);
        }
    }
}
