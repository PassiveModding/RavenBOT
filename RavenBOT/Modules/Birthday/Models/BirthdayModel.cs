using System;

namespace RavenBOT.Modules.Birthday.Models
{
    public class BirthdayModel
    {
        public static string DocumentName(ulong userId)
        {
            return $"Birthday-{userId}";
        }

        public BirthdayModel(ulong userId, DateTime birthday, bool showYear)
        {
            UserId = userId;
            Birthday = birthday;
            ShowYear = showYear;
        }
        public BirthdayModel(){}

        public ulong UserId {get;set;}
        public DateTime Birthday {get;set;}

        public bool ShowYear {get;set;}

        public int Attempts {get;set;} = 0;

        public int RemainingDays()
        {
            DateTime today = DateTime.Today;
            DateTime nextBirthday;
            if (today.DayOfYear > Birthday.DayOfYear)
            {
                nextBirthday = new DateTime(today.Year + 1, Birthday.Month, Birthday.Day);
            }
            else
            {
                nextBirthday = new DateTime(today.Year, Birthday.Month, Birthday.Day);
            }

            int remainingDays = nextBirthday.DayOfYear - today.DayOfYear;
            return remainingDays;
        }

        public bool IsToday()
        {
            DateTime today = DateTime.Today;
            
            DateTime birthdayThisYear = Birthday.AddYears(today.Year - Birthday.Year);

            if (birthdayThisYear < today)
            {
                birthdayThisYear = birthdayThisYear.AddYears(1);
            }

            double days = (birthdayThisYear - today).TotalDays;
            bool isToday =  days <= 1;

            return isToday;
        }

        public int Age()
        {
            if (ShowYear)
            {
                DateTime today = DateTime.Today;
                int age = today.Year - Birthday.Year;
                if (today < Birthday.AddYears(age))
                {
                    age--;
                }

                if (age <= 0)
                {
                    return -1;
                }

                return age;
            }

            return -1;
        }
    }
}