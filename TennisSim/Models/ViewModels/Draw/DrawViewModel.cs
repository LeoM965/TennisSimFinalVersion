using TennisSim.Models.DTOs.Draw;

namespace TennisSim.Models.ViewModels.Draw
{

    public class DrawViewModel
    {
        public DrawDto Draw { get; set; }
        public string TournamentName { get; set; }
        public string DrawMessage { get; set; }
        public bool IsDrawAvailable => Draw != null;
        public bool ShowMessage => !string.IsNullOrEmpty(DrawMessage);
    }
}
