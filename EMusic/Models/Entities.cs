using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMusic.Models
{
    public class MusicContext : DbContext
    {
        public MusicContext()
            : base("MusicContext")
        {
            Database.SetInitializer<MusicContext>(null);
        }

        public DbSet<Track> Tracks { get; set; }
        public DbSet<MusiсDir> MusiсDirs { get; set; }
        public DbSet<Setting> Settings { get; set; }
        public DbSet<DownloadOffset> DownloadOffsets { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();
        }

        //public void CreateDatabaseIfNotExists()
        //{
        //    Database.SetInitializer(new DropCreateDatabaseIfModelChanges<MusicContext>());
        //}
    }

    public class Track
    {
        [Key]
        public long TrackID { get; set; }
        public long Tid { get; set; }
        public long MusicDirID { get; set; }
        public string TrackName { get; set; }
        public string TrackAuthor { get; set; }
        public long TrackDuration { get; set; }
        public string TrackFileName { get; set; }
        public string TrackUrl { get; set; }
        public int TrackSave { get; set; }

        [NotMapped]
        public byte[] Body { get; set; }

        public virtual MusiсDir MusiсDir { get; set; }
    }

    public class MusiсDir
    {
        [Key]
        public long MusicDirID { get; set; }
        public long Gid { get; set; }
        public string NameDir { get; set; }
        public int PositiveRating { get; set; }
        public int NegativeRating { get; set; }
        public int CloseForSearch { get; set; }
        public virtual ICollection<Track> Tracks { get; set; }
        public virtual ICollection<DownloadOffset> DownloadOffsets { get; set; }
    }

    public class Setting
    {
        [Key] 
        public long SettingID { get; set; }
        public string Key { get; set; }
        public string Value { get; set; }
    }

    public class DownloadOffset
    {
        [Key]
        public long DownloadOffsetID { get; set; }
        public long MusicDirID { get; set; }
        public long Offset { get; set; }

        public virtual MusiсDir MusiсDir { get; set; }
    }
}
