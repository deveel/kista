namespace Kista;

[Trait("Category", "Unit")]
[Trait("Layer", "Infrastructure")]
[Trait("Feature", "GeoPoint")]
public class GeoPointTests
{
	[Fact]
	public void Constructor_ShouldSetLatitudeAndLongitude()
	{
		// Arrange & Act
		var point = new GeoPoint(41.9028, 12.4964);

		// Assert
		Assert.Equal(41.9028, point.Latitude);
		Assert.Equal(12.4964, point.Longitude);
	}

	[Fact]
	public void ToString_ShouldReturnFormattedString()
	{
		// Arrange
		var point = new GeoPoint(41.9028, 12.4964);

		// Act
		var result = point.ToString();

		// Assert
		Assert.Equal("41.9028,12.4964", result);
	}

	[Fact]
	public void Constructor_ShouldHandleNegativeValues()
	{
		// Arrange & Act
		var point = new GeoPoint(-33.8688, 151.2093);

		// Assert
		Assert.Equal(-33.8688, point.Latitude);
		Assert.Equal(151.2093, point.Longitude);
	}

	[Fact]
	public void Constructor_ShouldHandleZeroValues()
	{
		// Arrange & Act
		var point = new GeoPoint(0, 0);

		// Assert
		Assert.Equal(0, point.Latitude);
		Assert.Equal(0, point.Longitude);
	}
}
