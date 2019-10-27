using System;
using System.Collections.Generic;
using System.Linq;

namespace RavenBOT.Common
{
    public class LicenseService
    {
        public IDatabase Store { get; }
        private Random Random { get; }

        public LicenseService(IDatabase store)
        {
            Store = store;
            Random = new Random();
        }

        public TimedUserProfile GetTimedUser(string type, ulong userId)
        {
            var uProfile = Store.Load<TimedUserProfile>($"TimedProfile-{type}-{userId}");

            if (uProfile != null) return uProfile;

            //if the user profile is non-existent, create it and add it to the database
            uProfile = new TimedUserProfile(type, userId);
            Store.Store(uProfile, $"TimedProfile-{type}-{userId}");
            return uProfile;
        }

        public QuantifiableUserProfile GetQuantifiableUser(string type, ulong userId)
        {
            var uProfile = Store.Load<QuantifiableUserProfile>($"QuantifiableProfile-{type}-{userId}");

            if (uProfile != null) return uProfile;

            //if the user profile is non-existent, create it and add it to the database
            uProfile = new QuantifiableUserProfile(type, userId);
            Store.Store(uProfile, $"QuantifiableProfile-{type}-{userId}");
            return uProfile;
        }

        public void SaveUser(QuantifiableUserProfile profile)
        {
            Store.Store(profile, $"QuantifiableProfile-{profile.ProfileType}-{profile.UserId}");
        }
        public void SaveUser(TimedUserProfile profile)
        {
            Store.Store(profile, $"TimedProfile-{profile.ProfileType}-{profile.UserId}");
        }

        public enum RedemptionResult
        {
            AlreadyClaimed,
            InvalidKey,
            Success
        }

        //Returns whether the operation was successful
        public RedemptionResult RedeemLicense(TimedUserProfile profile, string key)
        {
            var license = key != null ? Store.Load<TimedLicense>($"TimedLicense-{profile.ProfileType}-{key}") : null;

            if (license == null)
            {
                return RedemptionResult.InvalidKey;
            }

            if (license.RedemptionDate != null)
            {
                return RedemptionResult.AlreadyClaimed;
            }

            profile.RedeemLicense(license);
            Store.Store(profile, $"TimedProfile-{profile.ProfileType}-{profile.UserId}");
            Store.Store(license, $"TimedLicense-{profile.ProfileType}-{key}");
            return RedemptionResult.Success;
        }

        public RedemptionResult RedeemLicense(QuantifiableUserProfile profile, string key)
        {
            var license = Store.Load<QuantifiableLicense>($"QuantifiableLicense-{profile.ProfileType}-{key}");

            if (license == null)
            {
                return RedemptionResult.InvalidKey;
            }

            if (license.RedemptionDate != null)
            {
                return RedemptionResult.AlreadyClaimed;
            }

            profile.RedeemLicense(license);
            Store.Store(profile, $"QuantifiableProfile-{profile.ProfileType}-{profile.UserId}");
            Store.Store(license, $"QuantifiableLicense-{profile.ProfileType}-{key}");
            return RedemptionResult.Success;
        }

        public List<QuantifiableLicense> MakeLicenses(string type, int amount, int uses)
        {
            var oldLicenses = Store.Query<QuantifiableLicense>().Where(x => x.LicenseType.Equals(type)).ToList();
            var newLicenses = new List<QuantifiableLicense>();

            for (int i = 0; i < amount; i++)
            {
                var newLicense = MakeQuantifiableLicense(type, uses);

                //Avoid creating licenses with duplicate keys.
                if (oldLicenses.Any(x => x.Key.Equals(newLicense.Key)))
                {
                    i--;
                }
                else
                {
                    newLicenses.Add(newLicense);
                }
            }

            Store.StoreMany(newLicenses, x => $"QuantifiableLicense-{x.LicenseType}-{x.Key}");

            return newLicenses;
        }

        //Returns the new licenses that have been created.
        public List<TimedLicense> MakeLicenses(string type, int amount, TimeSpan time)
        {
            var oldLicenses = Store.Query<TimedLicense>().Where(x => x.LicenseType.Equals(type)).ToList();
            var newLicenses = new List<TimedLicense>();

            for (int i = 0; i < amount; i++)
            {
                var newLicense = MakeTimedLicense(type, time);

                //Avoid creating licenses with duplicate keys.
                if (oldLicenses.Any(x => x.Key.Equals(newLicense.Key)))
                {
                    i--;
                }
                else
                {
                    newLicenses.Add(newLicense);
                }
            }

            Store.StoreMany(newLicenses, x => $"TimedLicense-{x.LicenseType}-{x.Key}");

            return newLicenses;
        }

        private QuantifiableLicense MakeQuantifiableLicense(string type, int uses)
        {
            var license = new QuantifiableLicense($"{GenerateRandomNo()}-{GenerateRandomNo()}-{GenerateRandomNo()}-{GenerateRandomNo()}", type, uses);
            return license;
        }

        private TimedLicense MakeTimedLicense(string type, TimeSpan time)
        {
            var license = new TimedLicense($"{GenerateRandomNo()}-{GenerateRandomNo()}-{GenerateRandomNo()}-{GenerateRandomNo()}", type, time);
            return license;
        }

        private string GenerateRandomNo()
        {
            return Random.Next(0, 9999).ToString("D4");
        }

        public class QuantifiableUserProfile
        {
            public QuantifiableUserProfile(string type, ulong userId)
            {
                ProfileType = type;
                Prefix = "QuantifiableProfile";
                Id = $"{Prefix}-{type}-{userId}";
                UserId = userId;
                TotalUsed = 0;
                Licenses = new List<QuantifiableLicense>();
                History = new Dictionary<long, string>();
                UpdateHistory("User Profile Generated");
            }

            public QuantifiableUserProfile() { }

            public string Prefix { get; set; }

            //Note that ravenDB automatically uses the Id property for document names.
            public string Id { get; set; }

            public string ProfileType { get; set; }

            public ulong UserId { get; set; }

            public List<QuantifiableLicense> Licenses { get; set; }

            public int TotalUsed { get; set; }

            public Dictionary<long, string> History { get; set; } = new Dictionary<long, string>();


            public void UpdateHistory(string info)
            {
                History.Add(DateTime.UtcNow.Ticks, info);
            }

            public int RemainingUses()
            {
                return Licenses.OfType<QuantifiableLicense>().Sum(x => x.Uses) - TotalUsed;
            }

            public bool Use(int amount = 1, string reason = null)
            {
                //Ensure that an invalid amount isn't supplied
                if (amount <= 0)
                {
                    return false;
                }

                //Check to see if the amount can be deducted from the remaining uses
                if (RemainingUses() - amount < 0) return false;

                //Increment the amount used
                TotalUsed += amount;
                UpdateHistory($"Used {amount} uses for: {reason ?? "UNKNOWN"}");
                return true;
            }

            public bool UseNoLog(int amount = 1)
            {
                //Ensure that an invalid amount isn't supplied
                if (amount <= 0)
                {
                    return false;
                }

                //Check to see if the amount can be deducted from the remaining uses
                if (RemainingUses() - amount < 0) return false;

                //Increment the amount used
                TotalUsed += amount;
                return true;
            }

            //NOTE: This should only ever be used by the QuantifiableLicenseService class.
            public bool RedeemLicense(QuantifiableLicense quantifiableLicense)
            {
                if (quantifiableLicense.RedemptionDate != null)
                {
                    return false;
                }

                quantifiableLicense.RedemptionDate = DateTime.UtcNow;
                Licenses.Add(quantifiableLicense);
                UpdateHistory($"Redeemed License {quantifiableLicense.Key} with {quantifiableLicense.Uses} uses. Total Balance = {RemainingUses()}");
                return true;
            }
        }

        public class TimedUserProfile
        {
            public TimedUserProfile(string type, ulong userId)
            {
                ProfileType = type;
                Prefix = "TimedProfile";
                Id = $"{Prefix}-{type}-{userId}";
                UserId = userId;
                Licenses = new List<TimedLicense>();
                History = new Dictionary<long, string>();
                UpdateHistory("User Profile Generated");
                ExpireTime = DateTime.MinValue;
            }
            public TimedUserProfile() { }

            public string Prefix { get; set; }

            //Note that ravenDB automatically uses the Id property for document names.
            public string Id { get; set; }

            public ulong UserId { get; set; }

            public string ProfileType { get; set; }

            public List<TimedLicense> Licenses { get; set; }

            public Dictionary<long, string> History { get; set; }

            public void UpdateHistory(string info)
            {
                History.Add(DateTime.UtcNow.Ticks, info);
            }

            public bool RedeemLicense(TimedLicense timedLicense)
            {
                if (timedLicense is TimedLicense timed)
                {
                    if (timed.RedemptionDate != null)
                    {
                        return false;
                    }

                    timed.RedemptionDate = DateTime.UtcNow;
                    Licenses.Add(timedLicense);

                    if (ExpireTime <= DateTime.UtcNow)
                    {
                        ExpireTime = DateTime.UtcNow + timed.Length;
                    }
                    else
                    {
                        ExpireTime = ExpireTime + timed.Length;
                    }

                    UpdateHistory($"Redeemed License {timed.Key} with {GetReadableLength(timed.Length)}. Time Remaining: {GetReadableLength(ExpireTime - DateTime.UtcNow)}");

                    return true;
                }

                return false;
            }

            private DateTime ExpireTime { get; set; }

            public DateTime GetExpireTime()
            {
                return ExpireTime;
            }

            public bool Expired(string usedFor = null)
            {
                if (ExpireTime > DateTime.UtcNow)
                {
                    if (usedFor != null)
                    {
                        UpdateHistory(usedFor);
                    }
                    return false;
                }

                return true;
            }
        }

        public class TimedLicense
        {
            public TimedLicense(string type, string key, TimeSpan length)
            {
                Prefix = "TimedLicense";
                Id = $"{Prefix}-{type}-{key}";
                Key = key;
                Length = length;
                RedemptionDate = null;
                CreationDate = DateTime.UtcNow;
                LicenseType = type;
            }
            public TimedLicense() { }

            public string Id { get; set; }

            public string Key { get; set; }
            public string LicenseType { get; set; }
            public string Prefix { get; set; }

            public TimeSpan Length { get; set; }

            public DateTime CreationDate { get; set; }
            public DateTime? RedemptionDate { get; set; }
        }

        public class QuantifiableLicense
        {
            public QuantifiableLicense(string key, string type, int uses)
            {
                Prefix = "QuantifiableLicense";
                Id = $"{Prefix}-{type}-{key}";
                Key = key;
                Uses = uses;
                RedemptionDate = null;
                CreationDate = DateTime.UtcNow;
                LicenseType = type;
            }
            public QuantifiableLicense() { }

            public string Id { get; set; }

            public string LicenseType { get; set; }
            public string Prefix { get; set; }

            public string Key { get; set; }
            public int Uses { get; set; }
            public DateTime CreationDate { get; set; }
            public DateTime? RedemptionDate { get; set; }
        }

        public static string GetReadableLength(TimeSpan length)
        {
            int days = (int)length.TotalDays;
            int hours = (int)length.TotalHours - days * 24;
            int minutes = (int)length.TotalMinutes - days * 24 * 60 - hours * 60;
            int seconds = (int)length.TotalSeconds - days * 24 * 60 * 60 - hours * 60 * 60 - minutes * 60;

            return $"{(days > 0 ? $"{days} Day(s) " : "")}{(hours > 0 ? $"{hours} Hour(s) " : "")}{(minutes > 0 ? $"{minutes} Minute(s) " : "")}{(seconds > 0 ? $"{seconds} Second(s)" : "")}";
        }
    }
}