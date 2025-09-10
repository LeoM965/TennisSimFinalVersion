using TennisSim.Models.Entities;

namespace TennisSim.Services.EntryListS
{
    public static class TournamentConfiguration
    {
        public static int GetMaxEntries(TournamentCategory category) => category switch
        {
            TournamentCategory.GrandSlam => 128,
            TournamentCategory.Masters1000 => 96,
            TournamentCategory.Masters500 => 48,
            _ => 32
        };

        public static bool IsEligible(int rank, TournamentCategory category) => category switch
        {
            TournamentCategory.GrandSlam or TournamentCategory.Masters1000 => rank <= 196,
            TournamentCategory.Masters500 => rank <= 160,
            TournamentCategory.Masters250 => rank <= 170,
            TournamentCategory.Challenger => rank is >= 30 and <= 250,
            TournamentCategory.ITF100K => rank is >= 50 and <= 400,
            TournamentCategory.ITF50K => rank is >= 150 and <= 400,
            TournamentCategory.ITF25K or TournamentCategory.ITF10K => rank is >= 200 and <= 400,
            _ => true
        };

        public static double CalculateEntryChance(int rank, TournamentCategory category) => category switch
        {
            TournamentCategory.GrandSlam => rank <= 50 ? 1.0 : rank <= 100 ? 0.95 : rank <= 196 ? 0.8 : 0.0,

            TournamentCategory.Masters1000 => rank <= 30 ? 1.0 : rank <= 80 ? 0.9 : rank <= 196 ? 0.6 : 0.0,

            TournamentCategory.Masters500 => rank <= 40 ? 0.65 : rank <= 90 ? 0.7 : rank <= 196 ? 0.5 : 0.25,

            TournamentCategory.Masters250 => rank switch
            {
                <= 20 => 0.1,
                <= 39 => 0.2,
                <= 79 => 0.3,
                <= 99 => 0.4,
                <= 120 => 0.45,
                <= 170 => 0.5,
                _ => 0.0
            },

            TournamentCategory.Challenger => rank switch
            {
                >= 30 and <= 80 => 0.1,
                >= 81 and <= 100 => 0.7,
                >= 101 and <= 150 => 0.5,
                >= 151 and <= 250 => 0.7,
                _ => 0.0
            },

            TournamentCategory.ITF100K => rank switch
            {
                >= 50 and <= 99 => 0.05,
                >= 100 and <= 149 => 0.45,
                >= 150 and <= 250 => 0.45,
                >= 251 and <= 350 => 0.6,
                >= 351 and <= 400 => 0.4,
                _ => 0.0
            },

            TournamentCategory.ITF50K => rank switch
            {
                >= 200 and <= 300 => 0.7,
                >= 301 and <= 350 => 0.6,
                >= 351 and <= 400 => 0.2,
                _ => 0.0
            },

            TournamentCategory.ITF25K => rank switch
            {
                >= 200 and <= 249 => 0.15,
                >= 250 and <= 350 => 0.6,
                >= 351 and <= 400 => 0.4,
                _ => 0.0
            },

            TournamentCategory.ITF10K => rank switch
            {
                >= 300 and <= 330 => 0.1,
                >= 331 and <= 400 => 1.0,
                _ => 0.0
            },

            _ => 0.0
        };
    }
}