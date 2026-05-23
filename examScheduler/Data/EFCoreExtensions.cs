using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Util.Extensions;

namespace examScheduler.Data;

public static class EFCoreExtensions
{
	public static void ConfigureIDGeneratedClientside<TEntity>(this EntityTypeBuilder<TEntity> builder) where TEntity : class, IGuidEntity => builder.Property(x => x.Id).ValueGeneratedNever();
}
