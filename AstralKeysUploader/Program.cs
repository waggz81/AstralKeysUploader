using System;
using System.Collections.Generic;
using System.Windows.Forms;


namespace AstralKeysUploader
{
    // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse); 
    public class Scores
    {
        public string all { get; set; }
        public string dps { get; set; }
        public string tank { get; set; }
        public string healer { get; set; }
    }

    public class MythicPlusScoresBySeason
    {
        public Scores scores { get; set; }
    }

    public class Guild
    {
        public string name { get; set; }
        //public string realm { get; set; }
    }

    public class RIOProfile
    {
        public string name { get; set; }
        public string @class { get; set; }
        public string active_spec_name { get; set; }
        public string active_spec_role { get; set; }
        public List<MythicPlusScoresBySeason> mythic_plus_scores_by_season { get; set; }
        public Guild guild { get; set; }
    }
    
    public class Keystone
    {
        public string character { get; set; }
        public RIOProfile RIOProfile { get; set; }
        public int key_level { get; set; }
        
        //public int week { get; set; }
        public int time_stamp { get; set; }
        public int dungeon_id { private get; set; }
        public string dungeon_name { 
            get {
                return dungeonList[dungeon_id];
            }
        }
        private Dictionary<int, string> dungeonList = new Dictionary<int, string>
        {
                {375, "Mists of Tirna Scithe"},
                {376, "The Necrotic Wake"},
                {377, "De Other Side"},
                {378, "Halls of Atonement"},
                {379, "Plaguefall"},
                {380, "Sanguine Depths"},
                {381, "Spires of Ascension"},
                {382, "Theater of Pain"}
        };
     
    }

    public class SubmissionData
    {
        public List<Keystone> keystones { get; set; }
        public string user { get; set; }

    }

    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }
    }
}
