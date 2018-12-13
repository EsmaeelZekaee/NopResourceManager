namespace ResUtility.Nop.Map
{
    /// <summary>
    /// Mapping class
    /// </summary>
    public partial class LocaleStringResourceMap : System.Data.Entity.ModelConfiguration.EntityTypeConfiguration<LocaleStringResource>
    {
        /// <summary>
        /// Ctor
        /// </summary>
        public LocaleStringResourceMap()
        {
            this.ToTable("LocaleStringResource");
            this.HasKey(lsr => lsr.Id);
            this.Property(lsr => lsr.ResourceName).IsRequired().HasMaxLength(200);
            this.Property(lsr => lsr.ResourceValue).IsRequired();

            //this.HasRequired(lsr => lsr.Language)
            //    .WithMany()
            //    .HasForeignKey(lsr => lsr.LanguageId);
        }
    }
}