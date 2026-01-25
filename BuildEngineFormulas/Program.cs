using BuildEngineFormulas;

Random r = new Random();

for (int i = 0; i < 512; i++) {
    uint val = (uint)r.Next();
    Console.WriteLine($"{val}: {Math.Sqrt(val)} {SquareRoot.Lookup(val)}");
}