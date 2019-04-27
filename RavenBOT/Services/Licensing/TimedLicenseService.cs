using System;
using System.Collections.Generic;
using System.Linq;
using Raven.Client.Documents;

namespace RavenBOT.Services.Licensing
{
    public class TimedLicenseService
    {
        public IDocumentStore Store { get; }
        private Random Random { get; }

        public TimedLicenseService(IDocumentStore store)
        {
            Store = store;
            Random = new Random();
        }

        public TimedUserProfile GetUser(ulong userId)
        {
            using (var session = Store.OpenSession())
            {
                var uProfile = session.Load<TimedUserProfile>($"TimedProfile-{userId}");

                if (uProfile != null) return uProfile;

                //if the user profile is non-existent, create it and add it to the database
                uProfile = new TimedUserProfile(userId);
                session.Store(uProfile);
                session.SaveChanges();
                return uProfile;
            }
        }

        public void SaveUser(TimedUserProfile profile)
        {
            using (var session = Store.OpenSession())
            {
                session.Store(profile);
                session.SaveChanges();
            }
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
            using (var session = Store.OpenSession())
            {
                var license = key != null ? session.Load<TimedLicense>($"TimedLicense-{key}") : null;

                if (license == null)
                {
                    return RedemptionResult.InvalidKey;
                }

                if (license.RedemptionDate != null)
                {
                    return RedemptionResult.AlreadyClaimed;
                }

                profile.RedeemLicense(license);
                session.Store(profile);
                session.Store(license);
                session.SaveChanges();
                return RedemptionResult.Success;
            }
        }

        //Returns the new licenses that have been created.
        public List<TimedLicense> MakeLicenses(int amount, TimeSpan time)
        {
            using (var session = Store.OpenSession())
            {
                var oldLicenses = session.Query<TimedLicense>().ToList();
                var newLicenses = new List<TimedLicense>();

                for (int i = 0; i < amount; i++)
                {
                    var newLicense = MakeLicense(time);

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

                foreach (var license in newLicenses)
                {
                    session.Store(license);
                }

                session.SaveChanges();
                return newLicenses;
            }
        }

        private TimedLicense MakeLicense(TimeSpan time)
        {
            var license = new TimedLicense($"{GenerateRandomNo()}-{GenerateRandomNo()}-{GenerateRandomNo()}-{GenerateRandomNo()}", time);
            return license;
        }

        private string GenerateRandomNo()
        {
            return Random.Next(0, 9999).ToString("D4");
        }


        public class TimedUserProfile
        {
            public TimedUserProfile(ulong userId)
            {
                Id = $"TimedProfile-{userId}";
                UserId = userId;
                Licenses = new List<TimedLicense>();
                UserHistory = new Dictionary<DateTime, string>();
                UpdateHistory("User Profile Generated");
                ExpireTime = DateTime.MinValue;
            }

            public string Id { get; set; }

            public ulong UserId { get; }

            private List<TimedLicense> Licenses { get; }

            public Dictionary<DateTime, string> UserHistory { get; set; }

            public void UpdateHistory(string info)
            {
                UserHistory.Add(DateTime.UtcNow, info);
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

            //NOTE: This should only ever be used by the TimedLicenseService class.
            protected internal bool RedeemLicense(TimedLicense timedLicense)
            {
                if (timedLicense.RedemptionDate != null)
                {
                    return false;
                }

                timedLicense.RedemptionDate = DateTime.UtcNow;
                Licenses.Add(timedLicense);
                

                if (ExpireTime <= DateTime.UtcNow)
                {
                    ExpireTime = DateTime.UtcNow + timedLicense.Length;
                }
                else
                {
                    ExpireTime = ExpireTime + timedLicense.Length;
                }

                UpdateHistory($"Redeemed License {timedLicense.Key} with {GetReadableLength(timedLicense.Length)}. Time Remaining: {GetReadableLength(ExpireTime - DateTime.UtcNow)}");

                return true;
            }
        }

        public class TimedLicense
        {
            public TimedLicense(string key, TimeSpan length)
            {
                Id = $"TimedLicense-{key}";
                Key = key;
                Length = length;
                RedemptionDate = null;
                CreationDate = DateTime.UtcNow;
            }

            public string Id { get; set; }

            public string Key { get; set; }

            public TimeSpan Length { get; }

            public DateTime CreationDate { get; set; }
            public DateTime? RedemptionDate { get; set; }
        }

        public static string GetReadableLength(TimeSpan length)
        {
            int days = (int) length.TotalDays;
            int hours = (int) length.TotalHours - days * 24;
            int minutes = (int) length.TotalMinutes - days * 24 * 60 - hours * 60;
            int seconds = (int) length.TotalSeconds - days * 24 * 60 * 60 - hours * 60 * 60 - minutes * 60;

            return $"{(days > 0 ? $"{days} Day(s) " : "")}{(hours > 0 ? $"{hours} Hour(s) " : "")}{(minutes > 0 ? $"{minutes} Minute(s) " : "")}{(seconds > 0 ? $"{seconds} Second(s)" : "")}";
        }
    }
}
