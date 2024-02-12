using System;
using System.Text;
using System.ComponentModel.Design;
using System.Threading;
using MySql.Data.MySqlClient;
using System.Security.Cryptography;


class Program
{
    static void Main()
    {
        int menu = 1;
        string cs = @"server=localhost;userid=root;password=;database=jeux";
        using var con = new MySqlConnection(cs);
        con.Open();
        using var cmd = new MySqlCommand();
        cmd.Connection = con;
        // ! Si dans la bdd la table n'existe pas, créer la table
        // cmd.CommandText = @"CREATE TABLE jeuxdelavie(id INTEGER PRIMARY KEY AUTO_INCREMENT,pseudo TEXT, password TEXT)";
        // cmd.ExecuteNonQuery();
        // Console.WriteLine("Table jeuxdelavie created");

        while(menu == 1){
            // Option pour s'inscrire ou se connecter
            Console.WriteLine("1. S'inscrire");
            Console.WriteLine("2. Se connecter");
            int option = Convert.ToInt32(Console.ReadLine());

            if (option == 1){
                // Inscription
                Console.WriteLine("Entrez votre pseudo:");
                string ?pseudo = Console.ReadLine();
                Console.WriteLine("Entrez votre mot de passe:");
                string ?password = Console.ReadLine();
                Console.Clear();
                Console.WriteLine("Confirmer votre mot de passe:");
                string ?confirm_password = Console.ReadLine();
                Console.Clear();
                bool userExists = CheckUser(con, pseudo);
                if (userExists){
                    Console.WriteLine("L'utilisateur existe déjà.");
                }else{
                    if (password != confirm_password){
                        Console.WriteLine("Les mots de passe ne correspondent pas. Fin du programme.");
                    }else{
                        InsertUser(con, pseudo, password);
                        menu = 0;
                    }
                }
                // Insertion dans la base de données
            }else if (option == 2){
                // Connexion
                Console.WriteLine("Entrez votre pseudo:");
                string ?pseudo = Console.ReadLine();
                Console.WriteLine("Entrez votre mot de passe:");
                string ?password = Console.ReadLine();

                // Vérification dans la base de données
                bool userExists = CheckUser(con, pseudo, password);

                if (!userExists){
                    Console.WriteLine("Utilisateur non trouvé ou mot de passe incorrect.");
                }
            }else{
                Console.WriteLine("Option invalide.");
            }
        }


        // Sélectionne le nombre de ligne, colonne et le temps d'attente entre génération
        Console.WriteLine("Le nombre de ligne:");
        int rows = Convert.ToInt32(Console.ReadLine());
        Console.WriteLine("Le nombre de colonne:");
        int cols = Convert.ToInt32(Console.ReadLine());
        Console.WriteLine("Le temps d'attente entre génération (millisecondes):");
        int temps_attente = Convert.ToInt32(Console.ReadLine());
        
        
        // Initialiser la grille
        bool[,] grid = new bool[rows, cols];

        // Initialiser la grille avec des cellules vivantes
        InitializeGrid(grid);

        // Afficher la grille initiale
        PrintGrid(grid);

        // Lancer le jeu de la vie
        while (true)
        {
            Console.Clear();
            grid = NextGeneration(grid);
            PrintGrid(grid);
            Thread.Sleep(temps_attente); 
        }
    }

    // Initialiser la grille avec des cellules vivantes aléatoires
    static void InitializeGrid(bool[,] grid)
    {
        Random random = new Random();

        for (int i = 0; i < grid.GetLength(0); i++)
        {
            for (int j = 0; j < grid.GetLength(1); j++)
            {
                grid[i, j] = random.Next(2) == 0; // 50% de chance d'être vivant
            }
        }
    }

    // Afficher la grille dans la console
    static void PrintGrid(bool[,] grid)
    {
        for (int i = 0; i < grid.GetLength(0); i++)
        {
            for (int j = 0; j < grid.GetLength(1); j++)
            {
                Console.Write(grid[i, j] ? "■ " : "□ ");
            }
            Console.WriteLine();
        }
    }

    // Calculer la prochaine génération du jeu de la vie
    static bool[,] NextGeneration(bool[,] grid)
    {
        int rows = grid.GetLength(0);
        int cols = grid.GetLength(1);

        bool[,] newGrid = new bool[rows, cols];

        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < cols; j++)
            {
                int neighbors = CountNeighbors(grid, i, j);

                // Appliquer les règles du jeu de la vie
                if (grid[i, j])
                {
                    newGrid[i, j] = neighbors == 2 || neighbors == 3;
                }
                else
                {
                    newGrid[i, j] = neighbors == 3;
                }
            }
        }

        return newGrid;
    }

    // Compter le nombre de voisins vivants d'une cellule
    static int CountNeighbors(bool[,] grid, int x, int y)
    {
        int count = 0;
        int rows = grid.GetLength(0);
        int cols = grid.GetLength(1);

        for (int i = -1; i <= 1; i++)
        {
            for (int j = -1; j <= 1; j++)
            {
                int neighborX = x + i;
                int neighborY = y + j;

                if (neighborX >= 0 && neighborX < rows && neighborY >= 0 && neighborY < cols)
                {
                    if (i != 0 || j != 0)
                    {
                        count += grid[neighborX, neighborY] ? 1 : 0;
                    }
                }
            }
        }

        return count;
    }

        // Fonction pour insérer un utilisateur dans la base de données
    static void InsertUser(MySqlConnection con, string? pseudo, string? password)
    {
        // Hacher le mot de passe avec SHA-256
        string hashedPassword = HashPassword(password);
        using var cmd = new MySqlCommand();
        cmd.Connection = con;
        cmd.CommandText = $"INSERT INTO jeuxdelavie (pseudo, password) VALUES ('{pseudo}', '{hashedPassword}')";
        int T = cmd.ExecuteNonQuery();
        Console.WriteLine("Inscription réussie pour l'utilisateur " + pseudo + T);

    }

    // Fonction pour vérifier si un utilisateur existe dans la base de données
static bool CheckUser(MySqlConnection con, string? pseudo, string? password = null)
{
    using var cmd = new MySqlCommand();
    cmd.Connection = con;

    string sql = "SELECT pseudo FROM jeuxdelavie WHERE pseudo='root'";
    MySqlCommand cmds = new MySqlCommand(sql, con);
    MySqlDataReader rdr = cmds.ExecuteReader();

    while (rdr.Read())
    {
        Console.WriteLine(rdr[0]+" -- "+rdr[1]);
    }
    rdr.Close();


    if (string.IsNullOrWhiteSpace(password))
    {
        cmd.CommandText = $"SELECT COUNT(*) FROM jeuxdelavie WHERE pseudo = @pseudo";
        cmd.Parameters.AddWithValue("@pseudo", pseudo);
    }
    else
    {
        string hashedPassword = HashPassword(password);
        cmd.CommandText = $"SELECT COUNT(*) FROM jeuxdelavie WHERE pseudo = @pseudo AND password = @password";
        cmd.Parameters.AddWithValue("@pseudo", pseudo);
        cmd.Parameters.AddWithValue("@password", hashedPassword);
    }

    int count = Convert.ToInt32(cmd.ExecuteScalar());
    Console.WriteLine(count);
    return count > 0;
}

        // Fonction pour hacher un mot de passe avec SHA-256
    static string HashPassword(string password)
    {
        using (SHA256 sha256 = SHA256.Create())
        {
            byte[] hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            StringBuilder builder = new StringBuilder();

            foreach (byte b in hashedBytes)
            {
                builder.Append(b.ToString("x2"));
            }

            return builder.ToString();
        }
    }
}
