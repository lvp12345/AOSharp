using AOSharp.Common.GameData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AOLite
{
    [Serializable]
    public class CharacterSelect
    {
        public int AllowedCharacters;
        public ExpansionFlags Expansions;
        public List<Character> Characters;

        [Serializable]
        public class Character
        {
            public int Id;
            public string Name;

            public void Select()
            {
                Console.Title = Name;
                Client.CharacterName = Name;
                Client.SelectCharacter(Id);
            }
        }
    }
}
