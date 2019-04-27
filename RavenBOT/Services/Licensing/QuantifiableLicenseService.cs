using System;
using System.Collections.Generic;
using System.Linq;
using Raven.Client.Documents;

namespace RavenBOT.Services.Licensing
{
    public class QuantifiableLicenseService
    {
        public IDocumentStore Store { get; }
        private Random Random { get; }

        public QuantifiableLicenseService(IDocumentStore store)
        {
            Store = store;
            Random = new Random();
        }

        public QuantifiableUserProfile GetUser(ulong userId)
        {
            using (var session = Store.OpenSession())
            {
                var uProfile = session.Load<QuantifiableUserProfile>($"QuantifiableProfile-{userId}");

                if (uProfile != null) return uProfile;

                //if the user profile is non-existent, create it and add it to the database
                uProfile = new QuantifiableUserProfile(userId);
                session.Store(uProfile);
                session.SaveChanges();
                return uProfile;
            }
        }

        public void SaveUser(QuantifiableUserProfile profile)
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
        public RedemptionResult RedeemLicense(QuantifiableUserProfile profile, string key)
        {
            using (var session = Store.OpenSession())
            {
                var license = key != null ? session.Load<QuantifiableLicense>($"QuantifiableLicense-{key}") : null;

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
        public List<QuantifiableLicense> MakeLicenses(int amount, int uses)
        {
            using (var session = Store.OpenSession())
            {
                var oldLicenses = session.Query<QuantifiableLicense>().ToList();
                var newLicenses = new List<QuantifiableLicense>();

                for (int i = 0; i < amount; i++)
                {
                    var newLicense = MakeLicense(uses);

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

        private QuantifiableLicense MakeLicense(int uses)
        {
            var license = new QuantifiableLicense($"{GenerateRandomNo()}-{GenerateRandomNo()}-{GenerateRandomNo()}-{GenerateRandomNo()}", uses);
            return license;
        }

        private string GenerateRandomNo()
        {
            return Random.Next(0, 9999).ToString("D4");
        }


        public class QuantifiableUserProfile
        {
            public QuantifiableUserProfile(ulong userId)
            {
                Id = $"QuantifiableProfile-{userId}";
                UserId = userId;
                TotalUsed = 0;
                Licenses = new List<QuantifiableLicense>();
                UserHistory = new Dictionary<DateTime, string>();
                UpdateHistory("User Profile Generated");
            }

            public string Id { get; set; }

            public ulong UserId { get; }

            private List<QuantifiableLicense> Licenses { get; }

            public int TotalUsed { get; set; }

            public Dictionary<DateTime, string> UserHistory { get; set; }

            public void UpdateHistory(string info)
            {
                UserHistory.Add(DateTime.UtcNow, info);
            }

            public int RemainingUses()
            {
                return Licenses.Sum(x => x.Uses) - TotalUsed;
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

            //NOTE: This should only ever be used by the QuantifiableLicenseService class.
            protected internal bool RedeemLicense(QuantifiableLicense quantifiableLicense)
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

        public class QuantifiableLicense
        {
            public QuantifiableLicense(string key, int uses)
            {
                Id = $"QuantifiableLicense-{key}";
                Key = key;
                Uses = uses;
                RedemptionDate = null;
                CreationDate = DateTime.UtcNow;
            }

            public string Id { get; set; }

            public string Key { get; set; }
            public int Uses { get; set; }
            public DateTime CreationDate { get; set; }
            public DateTime? RedemptionDate { get; set; }
        }
    }
}
