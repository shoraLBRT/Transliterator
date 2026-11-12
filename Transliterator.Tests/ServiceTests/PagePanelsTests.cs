using Transliterator.Core.Services;
using Xunit;

namespace Transliterator.Tests.ServiceTests
{
    /// <summary>
    /// Панели страницы (D6): на широком экране они стоят колонкой сбоку без подложки,
    /// поэтому открыта всегда одна — вторая кнопка сменяет панель, а не кладёт колонку на колонку.
    /// </summary>
    public class PagePanelsTests
    {
        [Fact]
        public void OpeningAPanel_ClosesTheOneOpenBefore()
        {
            var panels = new PagePanels();

            panels.Open("reading");
            panels.Open("profile");

            Assert.True(panels.IsOpen("profile"));
            Assert.False(panels.IsOpen("reading"));
            Assert.Equal("profile", panels.Current);
        }

        [Fact]
        public void Toggle_ClosesTheOpenPanel_AndOpensAClosedOne()
        {
            var panels = new PagePanels();

            panels.Toggle("surah");
            Assert.True(panels.IsOpen("surah"));

            panels.Toggle("surah");
            Assert.Null(panels.Current);
        }

        [Fact]
        public void Closing_LeavesAnotherPanelOpen()
        {
            // Панель, закрытая другой, при закрытии не должна гасить ту, что её сменила.
            var panels = new PagePanels();

            panels.Open("reading");
            panels.Open("profile");
            panels.Close("reading");

            Assert.True(panels.IsOpen("profile"));
        }

        [Fact]
        public void Changed_FiresOnlyWhenTheOpenPanelChanges()
        {
            var panels = new PagePanels();
            var changes = 0;
            panels.Changed += () => changes++;

            panels.Open("reading");
            panels.Open("reading");
            panels.Close("profile");
            panels.Close("reading");
            panels.Close("reading");

            Assert.Equal(2, changes);
        }
    }
}
