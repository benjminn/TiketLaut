using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.Generic;
using System.Linq;

namespace TiketLaut.Services
{
    /// <summary>
    /// Service untuk mengambil data cuaca dari OpenWeather API
    /// Free tier: 1,000 calls/day
    /// Dokumentasi: https://openweathermap.org/current
    /// </summary>
    public class MarineWeatherService
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        
        // ⚠️ IMPORTANT: Ganti dengan API Key kamu dari https://openweathermap.org/api
        private const string API_KEY = "9b9d5aaaab12674bcc8eb09a05b9aa3e";
        private const string CURRENT_WEATHER_URL = "https://api.openweathermap.org/data/2.5/weather";
        private const string FORECAST_URL = "https://api.openweathermap.org/data/2.5/forecast";

        public class MarineWeatherData
        {
            public double Latitude { get; set; }
            public double Longitude { get; set; }
            public DateTime Time { get; set; }
            public double Temperature { get; set; } // Celsius
            public double WindSpeed { get; set; } // m/s
            public double WindDirection { get; set; } // degrees
            public double Humidity { get; set; } // %
            public double Pressure { get; set; } // hPa
            public double Visibility { get; set; } // meters
            public string WeatherMain { get; set; } = "Unknown"; // Clear, Clouds, Rain, etc.
            public string WeatherDescription { get; set; } = "Unknown";
            public string WeatherCondition { get; set; } = "Unknown";
            public string SafetyLevel { get; set; } = "Unknown"; // Safe, Moderate, Dangerous
            
            // Estimasi wave height dari wind speed (rough calculation)
            // Formula: Beaufort scale approximation
            public double EstimatedWaveHeight => WindSpeed <= 5 ? WindSpeed * 0.2 : 
                                                  WindSpeed <= 10 ? WindSpeed * 0.3 :
                                                  WindSpeed * 0.4;
        }

        /// <summary>
        /// Get current weather data for specific coordinates
        /// </summary>
        public async Task<MarineWeatherData?> GetMarineWeatherAsync(double latitude, double longitude)
        {
            try
            {
                if (string.IsNullOrEmpty(API_KEY) || API_KEY == "YOUR_API_KEY_HERE")
                {
                    throw new Exception("⚠️ API Key belum diset!\n\n" +
                        "1. Daftar gratis di: https://openweathermap.org/api\n" +
                        "2. Copy API key\n" +
                        "3. Paste di MarineWeatherService.cs line 20\n" +
                        "4. Baca OPENWEATHER_SETUP_GUIDE.md untuk detail");
                }

                var url = $"{CURRENT_WEATHER_URL}?lat={latitude}&lon={longitude}&appid={API_KEY}&units=metric&lang=id";

                System.Diagnostics.Debug.WriteLine($"[MarineWeather] Fetching: {url.Replace(API_KEY, "***")}");

                var response = await _httpClient.GetAsync(url);
                
                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine($"[MarineWeather] Error: {error}");
                    
                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        throw new Exception("❌ API Key tidak valid! Pastikan API key sudah benar.");
                    }
                    
                    throw new Exception($"OpenWeather API Error: {response.StatusCode}");
                }

                var json = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"[MarineWeather] Response: {json}");

                var data = JsonSerializer.Deserialize<OpenWeatherResponse>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (data?.Main == null)
                {
                    System.Diagnostics.Debug.WriteLine("[MarineWeather] No data found");
                    return null;
                }

                var weatherData = new MarineWeatherData
                {
                    Latitude = latitude,
                    Longitude = longitude,
                    Time = DateTime.Now,
                    Temperature = data.Main.Temp,
                    WindSpeed = data.Wind?.Speed ?? 0,
                    WindDirection = data.Wind?.Deg ?? 0,
                    Humidity = data.Main.Humidity,
                    Pressure = data.Main.Pressure,
                    Visibility = data.Visibility,
                    WeatherMain = data.Weather?.FirstOrDefault()?.Main ?? "Unknown",
                    WeatherDescription = data.Weather?.FirstOrDefault()?.Description ?? "Unknown"
                };

                // Determine condition and safety based on wind speed
                weatherData.WeatherCondition = DetermineWeatherCondition(weatherData.WindSpeed);
                weatherData.SafetyLevel = DetermineSafetyLevel(weatherData.WindSpeed);

                System.Diagnostics.Debug.WriteLine($"[MarineWeather] Success: {weatherData.WeatherMain}, Wind: {weatherData.WindSpeed} m/s, Est. Wave: {weatherData.EstimatedWaveHeight:F1}m");
                return weatherData;
            }
            catch (HttpRequestException ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MarineWeather] HTTP Error: {ex.Message}");
                throw new Exception($"Gagal mengambil data cuaca: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MarineWeather] Error: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Get weather for specific date/time (current weather only with free tier)
        /// </summary>
        public async Task<MarineWeatherData?> GetMarineWeatherForDateAsync(double latitude, double longitude, DateTime targetTime)
        {
            // OpenWeather free tier hanya current weather, return current data
            return await GetMarineWeatherAsync(latitude, longitude);
        }

        /// <summary>
        /// Check sailing conditions for a route
        /// </summary>
        public async Task<(bool IsSafe, string Reason, MarineWeatherData? Data)> CheckSailingConditionsAsync(
            double startLat, double startLon,
            double endLat, double endLon,
            DateTime departureTime)
        {
            try
            {
                // Check weather at start point
                var startWeather = await GetMarineWeatherAsync(startLat, startLon);
                if (startWeather == null)
                {
                    return (false, "❌ Tidak dapat mengambil data cuaca pelabuhan keberangkatan", null);
                }

                // Check weather at end point
                var endWeather = await GetMarineWeatherAsync(endLat, endLon);
                if (endWeather == null)
                {
                    return (false, "❌ Tidak dapat mengambil data cuaca pelabuhan tujuan", null);
                }

                // Check safety based on wind speed and estimated waves
                var maxWindSpeed = Math.Max(startWeather.WindSpeed, endWeather.WindSpeed);
                var maxEstWave = Math.Max(startWeather.EstimatedWaveHeight, endWeather.EstimatedWaveHeight);

                if (maxWindSpeed > 15 || maxEstWave > 3.0)
                {
                    return (false, 
                        $"⚠️ Kondisi berbahaya! Angin: {maxWindSpeed:F1} m/s, Est. Gelombang: {maxEstWave:F1}m", 
                        startWeather);
                }

                if (maxWindSpeed > 10 || maxEstWave > 2.0)
                {
                    return (true, 
                        $"⚠️ Kondisi moderate. Angin: {maxWindSpeed:F1} m/s, Est. Gelombang: {maxEstWave:F1}m. Hati-hati!", 
                        startWeather);
                }

                return (true, 
                    $"✅ Kondisi aman. Angin: {maxWindSpeed:F1} m/s, Est. Gelombang: {maxEstWave:F1}m", 
                    startWeather);
            }
            catch (Exception ex)
            {
                return (false, $"❌ Error: {ex.Message}", null);
            }
        }

        /// <summary>
        /// Determine weather condition based on wind speed
        /// </summary>
        private string DetermineWeatherCondition(double windSpeed)
        {
            return windSpeed switch
            {
                <= 3 => "Calm (Tenang)",
                <= 7 => "Light Breeze (Sepoi-sepoi)",
                <= 10 => "Moderate (Sedang)",
                <= 15 => "Fresh Wind (Angin Segar)",
                <= 20 => "Strong Wind (Angin Kuat)",
                _ => "Dangerous (Berbahaya)"
            };
        }

        /// <summary>
        /// Determine safety level based on wind speed
        /// </summary>
        private string DetermineSafetyLevel(double windSpeed)
        {
            return windSpeed switch
            {
                <= 10 => "Safe",      // < 10 m/s
                <= 15 => "Moderate",  // 10-15 m/s
                _ => "Dangerous"      // > 15 m/s
            };
        }

        /// <summary>
        /// Get weather icon based on wind speed
        /// </summary>
        public string GetWeatherIcon(double windSpeed)
        {
            return windSpeed switch
            {
                <= 3 => "😌",   // Calm
                <= 7 => "🌤️",   // Light
                <= 10 => "⛅",  // Moderate
                <= 15 => "🌊",  // Fresh
                _ => "⚠️"      // Dangerous
            };
        }

        /// <summary>
        /// Get safety color for UI
        /// </summary>
        public string GetSafetyColor(string safetyLevel)
        {
            return safetyLevel switch
            {
                "Safe" => "#28A745",      // Green
                "Moderate" => "#FFC107",  // Yellow/Orange
                "Dangerous" => "#DC3545", // Red
                _ => "#6C757D"            // Gray
            };
        }

        /// <summary>
        /// Get 5-day / 3-hour forecast for a location
        /// Returns up to 40 timestamps (5 days * 8 times per day)
        /// </summary>
        public async Task<List<ForecastData>?> GetForecastAsync(double latitude, double longitude)
        {
            try
            {
                if (string.IsNullOrEmpty(API_KEY) || API_KEY == "YOUR_API_KEY_HERE")
                {
                    throw new Exception("⚠️ API Key belum diset!");
                }

                var url = $"{FORECAST_URL}?lat={latitude}&lon={longitude}&appid={API_KEY}&units=metric&lang=id";
                
                System.Diagnostics.Debug.WriteLine($"[MarineWeather] Fetching Forecast: {url.Replace(API_KEY, "***")}");

                var response = await _httpClient.GetAsync(url);
                
                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"Forecast API Error: {response.StatusCode}");
                }

                var json = await response.Content.ReadAsStringAsync();
                var forecastResponse = JsonSerializer.Deserialize<ForecastResponse>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (forecastResponse?.List == null || !forecastResponse.List.Any())
                {
                    return null;
                }

                // Convert to ForecastData list
                var forecasts = forecastResponse.List.Select(item => new ForecastData
                {
                    DateTime = DateTimeOffset.FromUnixTimeSeconds(item.Dt).LocalDateTime,
                    Temperature = item.Main?.Temp ?? 0,
                    WindSpeed = item.Wind?.Speed ?? 0,
                    WindDirection = item.Wind?.Deg ?? 0,
                    Humidity = item.Main?.Humidity ?? 0,
                    Pressure = item.Main?.Pressure ?? 0,
                    WeatherMain = item.Weather?.FirstOrDefault()?.Main ?? "Unknown",
                    WeatherDescription = item.Weather?.FirstOrDefault()?.Description ?? "Unknown",
                    Clouds = item.Clouds?.All ?? 0,
                    Pop = item.Pop ?? 0 // Probability of precipitation
                }).ToList();

                return forecasts;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MarineWeather] Forecast Error: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Check route with linear interpolation (5 waypoints)
        /// </summary>
        public async Task<RouteCheckResult> CheckRouteWithWaypointsAsync(
            double startLat, double startLon, string startName,
            double endLat, double endLon, string endName)
        {
            try
            {
                var waypoints = new List<WaypointWeather>();
                const int numberOfPoints = 5;

                for (int i = 0; i < numberOfPoints; i++)
                {
                    double t = i / (double)(numberOfPoints - 1); // 0, 0.25, 0.5, 0.75, 1.0
                    double lat = startLat + (endLat - startLat) * t;
                    double lon = startLon + (endLon - startLon) * t;

                    string pointName;
                    if (i == 0)
                        pointName = startName;
                    else if (i == numberOfPoints - 1)
                        pointName = endName;
                    else
                        pointName = $"Waypoint {i} ({(t * 100):F0}%)";

                    var weather = await GetMarineWeatherAsync(lat, lon);
                    
                    if (weather != null)
                    {
                        waypoints.Add(new WaypointWeather
                        {
                            PointName = pointName,
                            Latitude = lat,
                            Longitude = lon,
                            ProgressPercent = t * 100,
                            Weather = weather
                        });
                    }

                    // Delay to avoid rate limit
                    if (i < numberOfPoints - 1)
                        await Task.Delay(100);
                }

                if (!waypoints.Any())
                {
                    return new RouteCheckResult
                    {
                        IsSuccess = false,
                        Message = "❌ Tidak dapat mengambil data cuaca untuk rute ini"
                    };
                }

                // Determine overall safety
                var maxWind = waypoints.Max(w => w.Weather.WindSpeed);
                var maxWave = waypoints.Max(w => w.Weather.EstimatedWaveHeight);
                var worstPoint = waypoints.OrderByDescending(w => w.Weather.WindSpeed).First();

                string overallStatus;
                string overallIcon;
                bool isSafe;

                if (maxWind > 15 || maxWave > 4.5)
                {
                    overallStatus = "BERBAHAYA - TIDAK DISARANKAN";
                    overallIcon = "❌";
                    isSafe = false;
                }
                else if (maxWind > 10 || maxWave > 3.0)
                {
                    overallStatus = "MODERATE - HATI-HATI";
                    overallIcon = "⚠️";
                    isSafe = true;
                }
                else
                {
                    overallStatus = "AMAN UNTUK BERLAYAR";
                    overallIcon = "✅";
                    isSafe = true;
                }

                return new RouteCheckResult
                {
                    IsSuccess = true,
                    IsSafe = isSafe,
                    OverallStatus = overallStatus,
                    OverallIcon = overallIcon,
                    MaxWindSpeed = maxWind,
                    MaxWaveHeight = maxWave,
                    WorstPointName = worstPoint.PointName,
                    Waypoints = waypoints,
                    Message = $"{overallIcon} Route: {startName} → {endName}\nStatus: {overallStatus}"
                };
            }
            catch (Exception ex)
            {
                return new RouteCheckResult
                {
                    IsSuccess = false,
                    Message = $"❌ Error: {ex.Message}"
                };
            }
        }

        #region Data Models

        public class ForecastData
        {
            public DateTime DateTime { get; set; }
            public double Temperature { get; set; }
            public double WindSpeed { get; set; }
            public double WindDirection { get; set; }
            public double Humidity { get; set; }
            public double Pressure { get; set; }
            public string WeatherMain { get; set; } = "Unknown";
            public string WeatherDescription { get; set; } = "Unknown";
            public int Clouds { get; set; }
            public double Pop { get; set; } // Probability of precipitation (0-1)
            
            public double EstimatedWaveHeight => WindSpeed <= 5 ? WindSpeed * 0.2 : 
                                                  WindSpeed <= 10 ? WindSpeed * 0.3 :
                                                  WindSpeed * 0.4;
            
            public string SafetyLevel => WindSpeed switch
            {
                <= 10 => "Safe",
                <= 15 => "Moderate",
                _ => "Dangerous"
            };
        }

        public class WaypointWeather
        {
            public string PointName { get; set; } = "";
            public double Latitude { get; set; }
            public double Longitude { get; set; }
            public double ProgressPercent { get; set; }
            public MarineWeatherData Weather { get; set; } = new MarineWeatherData();
        }

        public class RouteCheckResult
        {
            public bool IsSuccess { get; set; }
            public bool IsSafe { get; set; }
            public string OverallStatus { get; set; } = "";
            public string OverallIcon { get; set; } = "";
            public double MaxWindSpeed { get; set; }
            public double MaxWaveHeight { get; set; }
            public string WorstPointName { get; set; } = "";
            public List<WaypointWeather> Waypoints { get; set; } = new List<WaypointWeather>();
            public string Message { get; set; } = "";
        }

        #endregion

        #region OpenWeather API Response Models

        private class OpenWeatherResponse
        {
            [JsonPropertyName("coord")]
            public Coord? Coord { get; set; }

            [JsonPropertyName("weather")]
            public List<Weather>? Weather { get; set; }

            [JsonPropertyName("main")]
            public Main? Main { get; set; }

            [JsonPropertyName("wind")]
            public Wind? Wind { get; set; }

            [JsonPropertyName("visibility")]
            public double Visibility { get; set; }

            [JsonPropertyName("dt")]
            public long Dt { get; set; }

            [JsonPropertyName("name")]
            public string? Name { get; set; }
        }

        private class Coord
        {
            [JsonPropertyName("lat")]
            public double Lat { get; set; }

            [JsonPropertyName("lon")]
            public double Lon { get; set; }
        }

        private class Weather
        {
            [JsonPropertyName("main")]
            public string? Main { get; set; }

            [JsonPropertyName("description")]
            public string? Description { get; set; }

            [JsonPropertyName("icon")]
            public string? Icon { get; set; }
        }

        private class Main
        {
            [JsonPropertyName("temp")]
            public double Temp { get; set; }

            [JsonPropertyName("feels_like")]
            public double FeelsLike { get; set; }

            [JsonPropertyName("temp_min")]
            public double TempMin { get; set; }

            [JsonPropertyName("temp_max")]
            public double TempMax { get; set; }

            [JsonPropertyName("pressure")]
            public double Pressure { get; set; }

            [JsonPropertyName("humidity")]
            public double Humidity { get; set; }
        }

        private class Wind
        {
            [JsonPropertyName("speed")]
            public double Speed { get; set; }

            [JsonPropertyName("deg")]
            public double Deg { get; set; }

            [JsonPropertyName("gust")]
            public double? Gust { get; set; }
        }

        // Forecast API Models
        private class ForecastResponse
        {
            [JsonPropertyName("list")]
            public List<ForecastItem>? List { get; set; }
        }

        private class ForecastItem
        {
            [JsonPropertyName("dt")]
            public long Dt { get; set; }

            [JsonPropertyName("main")]
            public Main? Main { get; set; }

            [JsonPropertyName("weather")]
            public List<Weather>? Weather { get; set; }

            [JsonPropertyName("clouds")]
            public Clouds? Clouds { get; set; }

            [JsonPropertyName("wind")]
            public Wind? Wind { get; set; }

            [JsonPropertyName("pop")]
            public double? Pop { get; set; }
        }

        private class Clouds
        {
            [JsonPropertyName("all")]
            public int All { get; set; }
        }

        #endregion
    }
}
