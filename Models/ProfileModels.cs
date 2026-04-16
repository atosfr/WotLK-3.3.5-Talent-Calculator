using System;
using System.Collections.Generic;

namespace WotLK_TalentCalculator_3._3._5.Models
{
    /// <summary>
    /// A single named build record.
    /// Stores which class it belongs to (ClassId/ClassName) and 
    /// the current rank arrays of all classes (Builds).
    /// </summary>
    public class Profile
    {
        /// <summary>Unique identifier — used for deletion and updates.</summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>Name given by the user.</summary>
        public string Name { get; set; } = "";

        /// <summary>ID of the class active when the profile was saved.</summary>
        public string ClassId { get; set; } = "";

        /// <summary>Display name.</summary>
        public string ClassName { get; set; } = "";

        /// <summary>Spec distribution (e.g., "0/28/43").</summary>
        public string Distribution { get; set; } = "0/0/0";

        /// <summary>
        /// classId -> rank array.
        /// Contains all classes visited when the profile was saved.
        /// </summary>
        public Dictionary<string, string> Builds { get; set; } = new();

        // Glyphs
        public Dictionary<string, List<string>> MajorGlyphs { get; set; } = new();
        public Dictionary<string, List<string>> MinorGlyphs { get; set; } = new();

        /// <summary>Creation date/time (for display purposes).</summary>
        public string CreatedAt { get; set; } = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
    }

    /// <summary>Root object for Profiles.json.</summary>
    public class ProfileData
    {
        public List<Profile> Profiles { get; set; } = new();
    }
}