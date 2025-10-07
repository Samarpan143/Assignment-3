using System.Collections.Generic;
using System.Linq;


    
    public class CategoryService
    {
        private List<Category> _categories;
        private int _nextId = 4; 
        
        public CategoryService()
        {
            _categories = new List<Category>
            {
                new Category { Id = 1, Name = "Beverages" },
                new Category { Id = 2, Name = "Condiments" },
                new Category { Id = 3, Name = "Confections" }
            };
        }
        
        public List<Category> GetCategories()
        {
            return _categories.ToList(); 
        }
        
        public Category? GetCategory(int cid)
        {
            return _categories.FirstOrDefault(c => c.Id == cid);
        }
        
        public bool UpdateCategory(int id, string newName)
        {
            var category = _categories.FirstOrDefault(c => c.Id == id);
            if (category == null)
                return false;
                
            category.Name = newName;
            return true;
        }
        
        public bool DeleteCategory(int id)
        {
            var category = _categories.FirstOrDefault(c => c.Id == id);
            if (category == null)
                return false;
                
            _categories.Remove(category);
            return true;
        }
        
        public bool CreateCategory(int id, string name)
        {
            if (_categories.Any(c => c.Id == id))
                return false;
                
            _categories.Add(new Category { Id = id, Name = name });
            return true;
        }
        
        public Category CreateCategory(string name)
        {
            var category = new Category { Id = _nextId++, Name = name };
            _categories.Add(category);
            return category;
        }
    }
