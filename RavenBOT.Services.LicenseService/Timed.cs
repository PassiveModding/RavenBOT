using System;
using System.Collections.Generic;
using System.Linq;

namespace RavenBOT.Common.LicenseService
{
    public class Timed
    {
        public IDatabase Store { get; }

        public Timed(IDatabase store)
        {
            Store = store;
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

        public void SaveUser(TimedUserProfile profile)
        {
            Store.Store(profile, $"TimedProfile-{profile.ProfileType}-{profile.UserId}");
        }

        //Returns whether the operation was successful
        public LicenseService.RedemptionResult RedeemLicense(TimedUserProfile profile, string key)
        {
            var license = key != null ? Store.Load<TimedLicense>($"TimedLicense-{profile.ProfileType}-{key}") : null;

            if (license == null)
            {
                return LicenseService.RedemptionResult.InvalidKey;
            }

            if (license.RedemptionDate != null)
            {
                return LicenseService.RedemptionResult.AlreadyClaimed;
            }

            profile.RedeemLicense(license);
            Store.Store(profile, $"TimedProfile-{profile.ProfileType}-{profile.UserId}");
            Store.Store(license, $"TimedLicense-{profile.ProfileType}-{key}");
            return LicenseService.RedemptionResult.Success;
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

        private TimedLicense MakeTimedLicense(string type, TimeSpan time)
        {
            var license = new TimedLicense($"{LicenseService.GenerateRandomNo()}-{LicenseService.GenerateRandomNo()}-{LicenseService.GenerateRandomNo()}-{LicenseService.GenerateRandomNo()}", type, time);
            return license;
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

                    UpdateHistory($"Redeemed License {timed.Key} with {Extensions.GetReadableLength(timed.Length)}. Time Remaining: {Extensions.GetReadableLength(ExpireTime - DateTime.UtcNow)}");

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
    }
}
