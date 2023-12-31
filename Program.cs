using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Security.Principal;
using System.Text.RegularExpressions;

public abstract class Operand
{
    public abstract string ToTriadString();
}

public class VariableOperand : Operand
{
    public string Name { get; set; }

    public VariableOperand(string name)
    {
        Name = name;
    }

    public override string ToTriadString()
    {
        return $"{Name}";
    }
}

public class ConstantOperand : Operand
{
    public object Value { get; }

    public ConstantOperand(object value)
    {
        Value = value;
    }

    public override string ToTriadString()
    {
        return Value.ToString();
    }
    public bool IsNumeric(out double numericValue)
    {
        if (Value is int intValue)
        {
            numericValue = (double)intValue;
            return true;
        }
        else if (Value is double doubleValue)
        {
            numericValue = doubleValue;
            return true;
        }
        else if (Value is string stringValue && double.TryParse(stringValue, out double parsedValue))
        {
            numericValue = parsedValue;
            return true;
        }
        else
        {
            numericValue = 0;
            return false;
        }
    }
}

public abstract class Operator
{
    public abstract string ToTriadString(Operand left, Operand right);
}

public class AssignmentOperator : Operator
{
    public override string ToTriadString(Operand left, Operand right)
    {
        return $":= ({left.ToTriadString()} {right.ToTriadString()})";
    }
    public override string ToString()
    {
        return ":="; 
    }
}

public class AdditionOperator : Operator
{
    public override string ToTriadString(Operand left, Operand right)
    {
        return $"+ ({left.ToTriadString()} {right.ToTriadString()} )";
    }
    public override string ToString()
    {
        return "+"; 
    }
}

public class SubtractionOperator : Operator
{
    public override string ToTriadString(Operand left, Operand right)
    {
        return $"- ({left.ToTriadString()} {right.ToTriadString()})";
    }
    public override string ToString()
    {
        return "-"; 
    }
}

public class MultiplicationOperator : Operator
{
    public override string ToTriadString(Operand left, Operand right)
    {
        return $"* ({left.ToTriadString()}  {right.ToTriadString()})";
    }
    public override string ToString()
    {
        return "*"; 
    }
}


public class DivisionOperator : Operator
{
    public override string ToTriadString(Operand left, Operand right)
    {
        return $"/ ({left.ToTriadString()}  {right.ToTriadString()} )";
    }
    public override string ToString()
    {
        return "/"; 
    }
}

public class Triad
{
    private static int lineCount = 1;

    public int LineNumber { get; }
    public Operator Operator { get; }
    public Operand LeftOperand { get; set; }
    public Operand RightOperand { get; set; }
    public Operand Result { get; }

    public Triad(Operator op, Operand left, Operand right)
    {
        LineNumber = lineCount++;
        Operator = op;
        LeftOperand = left;
        RightOperand = right;
    }

    public bool IsLineNumberEqual(int number)
    {
        return LineNumber == number;
    }
    public Triad(Operator op, Operand left, Operand right, Operand result) : this(op, left, right)
    {
        Result = result;
    }

    public string ToTriadString()
    {
        if (Result != null)
        {
            return $"{Operator.ToTriadString(LeftOperand, RightOperand)}";
        }
        else
        {
            return $"{Operator.ToTriadString(LeftOperand, RightOperand)}";
        }
    }

    public Operand GetLeftOperand()
    {
        return LeftOperand;
    }
    public bool AreOperandsEqual(Triad other)
    {
        return (LeftOperand != null && other.LeftOperand != null && LeftOperand.ToTriadString() == other.LeftOperand.ToTriadString()) &&
               (RightOperand != null && other.RightOperand != null && RightOperand.ToTriadString() == other.RightOperand.ToTriadString());
    }
    public Operand GetRightOperand()
    {
        return RightOperand;
    }

    public void SetLeftOperand(Operand newLeftOperand)
    {
        LeftOperand = newLeftOperand;
    }

    public void SetRightOperand(Operand newRightOperand)
    {
        RightOperand = newRightOperand;
    }
}


public class ExpressionTreeNode
{
    public string Value { get; }
    public ExpressionTreeNode Left { get; set; }
    public ExpressionTreeNode Right { get; set; }

    public ExpressionTreeNode(string value)
    {
        Value = value;
        Left = null;
        Right = null;
    }

    public override string ToString()
    {
        return $"{Value}";
    }
}

public class TriadTree
{
    private readonly List<Triad> triads;
    private readonly Dictionary<Operand, ExpressionTreeNode> operandNodes;

    public TriadTree(List<Triad> triads)
    {
        this.triads = triads;
        this.operandNodes = new Dictionary<Operand, ExpressionTreeNode>();
    }

    public void PrintTriadTree()
    {
        var root = new ExpressionTreeNode("Root");

        foreach (var triad in triads)
        {
            var triadRoot = ConstructExpressionTree(triad);
            root.Left = MergeTrees(root.Left, triadRoot);
        }

        Console.WriteLine("Expression Tree:");
        PrintTree(root.Left);
    }

    private ExpressionTreeNode ConstructExpressionTree(Triad triad)
    {
        var root = new ExpressionTreeNode($"{triad.LineNumber}: {triad.ToTriadString()}");

        if (triad.LeftOperand != null)
        {
            var leftNode = FindOrCreateOperandNode(triad.LeftOperand);
            if (triad.LeftOperand is TemporaryVariableOperand tempLeft && tempLeft.NewValue > 0)
            {
                var tempTriad = triads.FirstOrDefault(t => t.LineNumber == tempLeft.NewValue);
                if (tempTriad != null)
                {
                    leftNode = ConstructExpressionTree(tempTriad);
                }
            }
            root.Left = leftNode;
        }

        if (triad.RightOperand != null)
        {
            var rightNode = FindOrCreateOperandNode(triad.RightOperand);
            if (triad.RightOperand is TemporaryVariableOperand tempRight && tempRight.NewValue > 0)
            {
                var tempTriad = triads.FirstOrDefault(t => t.LineNumber == tempRight.NewValue);
                if (tempTriad != null)
                {
                    rightNode = ConstructExpressionTree(tempTriad);
                }
            }
            root.Right = rightNode;
        }

        return root;
    }

    private ExpressionTreeNode FindOrCreateOperandNode(Operand operand)
    {
        if (!operandNodes.ContainsKey(operand))
        {
            operandNodes[operand] = CreateOperandNode(operand);
        }

        return operandNodes[operand];
    }

    private ExpressionTreeNode CreateOperandNode(Operand operand)
    {
        if (operand is VariableOperand variable)
        {
            return new ExpressionTreeNode(variable.Name);
        }
        else if (operand is ConstantOperand constant)
        {
            return new ExpressionTreeNode(constant.Value.ToString());
        }
        

        return null;
    }

    private ExpressionTreeNode MergeTrees(ExpressionTreeNode tree1, ExpressionTreeNode tree2)
    {
        if (tree1 == null)
        {
            return tree2;
        }

        var newRoot = new ExpressionTreeNode("Triad");
        newRoot.Left = tree1;
        newRoot.Right = tree2;
        return newRoot;
    }

    private void PrintTree(ExpressionTreeNode root, string indent = "", bool last = true)
    {
        if (root != null)
        {
            Console.Write(indent);
            if (last)
            {
                Console.Write("└─");
                indent += "  ";
            }
            else
            {
                Console.Write("├─");
                indent += "| ";
            }

            Console.WriteLine(root);

            if (root.Left != null)
            {
                PrintTree(root.Left, indent, root.Right == null);
            }

            if (root.Right != null)
            {
                PrintTree(root.Right, indent, true);
            }
        }
    }
}

public class Parser
{
    private readonly string input;
    private int index;

    private List<Triad> triads = new List<Triad>();
    
    public Parser(string input)
    {
        this.input = input;
        this.index = 0;
    }



    public void ParseAndOptimize()
    {
        Parse();


        Console.WriteLine("\nOriginal Triads:");
        PrintTriads(triads);


    }



    private void PrintTriads(List<Triad> triadList)
    {
        int count = 1;
        foreach (var triad in triadList)
        {
            Console.WriteLine($"{count} {triad.ToTriadString()}");
            count++;
        }
    }




    private void Parse()
    {
        try
        {
            S();
            Console.WriteLine("Парсинг успешен.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка парсинга: {ex.Message}");
        }
    }
    private void Match(string token)
    {
        if (index < input.Length && input.Substring(index).StartsWith(token))
        {
            index += token.Length;
        }
        else
        {
            throw new Exception($"Ожидался токен '{token}', но встречен '{input.Substring(index)}'");
        }
    }
    private Operand VariableOrConstant()
    {
        if (char.IsLetter(input[index]))
        {
            string variableName = input[index].ToString();
            index++;

            while (index < input.Length && (char.IsLetterOrDigit(input[index]) || input[index] == '_'))
            {
                variableName += input[index];
                index++;
            }

            return new VariableOperand(variableName);
        }
        else if (char.IsDigit(input[index]) || input[index] == '.')
        {
            string constantValue = input[index].ToString();
            index++;

            while (index < input.Length && (char.IsDigit(input[index]) || input[index] == '.'))
            {
                constantValue += input[index];
                index++;
            }

            return new ConstantOperand(double.Parse(constantValue));  
        }
        else if (input[index] == '\'')
        {
            char charValue = CHARCONST();
            return new ConstantOperand(charValue);
        }
        else
        {
            throw new Exception($"Неожиданный символ: {input[index]}");
        }
    }
    private void S()
    {
        F();
        Match(";");
    }
    private void F()
    {
        Operand leftOperand = VariableOrConstant();
        Match(":=");
        Operand rightOperand = T();

        Triad triad = new Triad(new AssignmentOperator(), leftOperand, rightOperand);
        triads.Add(triad);
    }
    private Operand T()
    {
        Operand result = E();
        while (index < input.Length && (input[index] == '+' || input[index] == '-'))
        {
            Operator op;
            if (input[index] == '+')
            {
                Match("+");
                op = new AdditionOperator();
            }
            else if (input[index] == '-')
            {
                Match("-");
                op = new SubtractionOperator();
            }
            else
            {
                throw new Exception($"Неожиданный символ: {input[index]}");
            }
            Operand rightOperand = E();
            result = ApplyOperator(op, result, rightOperand, false);
        }
        return result;
    }

    private Operand E()
    {
        Operand result = A();
        while (index < input.Length && (input[index] == '*' || input[index] == '/'))
        {
            Operator op;
            if (input[index] == '*')
            {
                Match("*");
                op = new MultiplicationOperator();
            }
            else if (input[index] == '/')
            {
                Match("/");
                op = new DivisionOperator();
            }
            else
            {
                throw new Exception($"Неожиданный символ: {input[index]}");
            }
            Operand rightOperand = A();
            result = ApplyOperator(op, result, rightOperand, false);
        }
        return result;
    }

    private Operand A()
    {
        if (input[index] == '(')
        {
            Match("(");
            Operand result = T();
            Match(")");
            return result;
        }
        else if (input[index] == '-')
        {
            Match("-");
            Operand operand = A();
            return new UnaryMinusOperator().Apply(operand);
        }
        else
        {
            return VariableOrConstant();
        }
    }

    private Operand ApplyOperator(Operator op, Operand left, Operand right, bool nujnoli)
    {
        Operand result = new TemporaryVariableOperand(); 
        Triad triad = new Triad(op, left, right, result);
        triads.Add(triad);
        return result;

    }
    private char CHARCONST()
    {
        if (input[index] == '\'')
        {
            index++;
        }
        else
        {
            throw new Exception($"Ожидалась одинарная кавычка");
        }

        if (index < input.Length && char.IsLetterOrDigit(input[index]))
        {
            char charValue = input[index];
            index++;

            if (index < input.Length && input[index] == '\'')
            {
                index++;
                return charValue;
            }
            else
            {
                throw new Exception($"Ожидалась одинарная кавычка");
            }
        }
        else
        {
            throw new Exception($"Ожидалась буква или цифра");
        }
    }

    static void Main()
    {
        string inputbd = "c:=(z+a-1)/2+('A'*'T');";
        string input = inputbd.Replace(" ", "");
        Parser parser = new Parser(input);
        parser.ParseAndOptimize();

        TriadTree triadTree = new TriadTree(parser.triads);
        triadTree.PrintTriadTree();
    }
}


public class TemporaryVariableOperand : Operand
{
    private static int count = 1;

    private string name;

    public int NewValue
    {
        get
        {
            return int.Parse(name.Substring(1));
        }
        set
        {
            name = $"^{value}";
        }
    }

    public TemporaryVariableOperand()
    {
        name = $"^{count++}";
    }

    public static TemporaryVariableOperand Create()
    {
        return new TemporaryVariableOperand();
    }

    public static TemporaryVariableOperand FromInt(int value)
    {
        return new TemporaryVariableOperand { NewValue = value };
    }


    public override string ToTriadString()
    {
        return name;
    }
}


public class UnaryMinusOperator : Operator
{
    public override string ToTriadString(Operand left, Operand right)
    {
        throw new NotImplementedException();
    }


    public Operand Apply(Operand operand)
    {
        return new UnaryMinusResultOperand(operand);
    }
}

public class UnaryMinusResultOperand : Operand
{
    private readonly Operand operand;

    public UnaryMinusResultOperand(Operand operand)
    {
        this.operand = operand;
    }

    public Operand Operand { get; internal set; }

    public override string ToTriadString()
    {
        return $"- {operand.ToTriadString()}";
    }

}
