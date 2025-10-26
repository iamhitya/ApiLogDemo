using ApiLogDemo.Models;
using System.Text.Json;

namespace ApiLogDemo.Services
{
    public class PeopleService
    {
        private readonly string _filePath;

        public PeopleService(IWebHostEnvironment environment)
        {
            _filePath = Path.Combine(environment.ContentRootPath, "Data", "people.json");
        }

        private List<Person> ReadFromFile()
        {
            if (!File.Exists(_filePath))
                return new List<Person>();

            var json = File.ReadAllText(_filePath);
            return JsonSerializer.Deserialize<List<Person>>(json) ?? new List<Person>();
        }

        private void WriteToFile(List<Person> people)
        {
            var json = JsonSerializer.Serialize(people, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_filePath, json);
        }

        public List<Person> GetAll()
        {
            var persons = ReadFromFile();
            AssignAttachement(persons);
            return persons;
        }

        public Person? GetById(int id)
        {
            return ReadFromFile().FirstOrDefault(p => p.Id == id);
        }

        public void Add(Person person)
        {
            var people = ReadFromFile();
            person.Id = people.Count > 0 ? people.Max(p => p.Id) + 1 : 1;
            people.Add(person);
            WriteToFile(people);
        }

        public void AddRange(IEnumerable<Person> persons)
        {
            if (persons == null) return;

            var people = ReadFromFile();
            var nextId = people.Any() ? people.Max(p => p.Id) + 1 : 1;

            foreach (var p in persons)
            {
                p.Id = nextId++;
                people.Add(p);
            }

            WriteToFile(people);
        }

        public bool Update(int id, Person updatedPerson)
        {
            var people = ReadFromFile();
            var person = people.FirstOrDefault(p => p.Id == id);
            if (person == null)
                return false;

            person.Name = updatedPerson.Name;
            person.Age = updatedPerson.Age;
            person.City = updatedPerson.City;

            WriteToFile(people);
            return true;
        }

        public bool Delete(int id)
        {
            var people = ReadFromFile();
            var person = people.FirstOrDefault(p => p.Id == id);
            if (person == null)
                return false;

            people.Remove(person);
            WriteToFile(people);
            return true;
        }

        #region Private Methods

        private void AssignAttachement(List<Person> persons)
        {
            var filesDir = Path.Combine(Environment.CurrentDirectory, "Files");
            var files = Directory.Exists(filesDir) ? Directory.GetFiles(filesDir) : Array.Empty<string>();

            foreach (var person in persons)
            {
                if (files.Length == 0)
                {
                    person.Attachement = Array.Empty<byte>();
                    continue;
                }

                var file = files[Random.Shared.Next(files.Length)];
                try
                {
                    person.Attachement = File.ReadAllBytes(file);
                }
                catch
                {
                    person.Attachement = Array.Empty<byte>();
                }
            }
        }

        #endregion
    }
}
