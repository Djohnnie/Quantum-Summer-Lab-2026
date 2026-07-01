// ============================================================
// CHALLENGE METADATA
// Edit the fields below directly. SolutionTemplate and
// VerificationTemplate are NOT stored here - they are derived
// automatically from the code below (see the SOLVE markers).
// After editing, run `python _challenges/generate_challenges.py`
// to regenerate QuantumSummerLab.Tools/Challenges.cs.
// ============================================================
// Name = "D3"
// Title = "Deutsch-Jozsa algorithm"
// Description = "You have to implement the Deutsch-Jozsa algorithm which determines whether a given oracle function is constant or balanced:[BR]If the oracle function is constant it returns 0 or 1 on all inputs.[BR]If the oracle function is balanced it returns 0 on half of the inputs and 1 on the other half.[BR]The oracle function is assumed to always be constant or balanced.[BR]Implement the Solve operation where n is the number of bits in the input register and oracle provides you with the oracle function that takes an input register and an output bit.[BR]The solve operation should return true if the oracle function is constant and false if it is balanced."
// Tldr = "You should implement the Deutsch-Jozsa algorithm in the empty Solve operation below and identify if the oracle function is constant or balanced."
// ExampleDescription = ""
// ExampleCode = ""
// ExpectedOutput = "true"
// ===EXPECTED-STATES-START===
// ===EXPECTED-STATES-END===
// CopilotInstructions = ""
// Level = 4
// ============================================================

import Std.Arithmetic.*;
import Std.Canon.*;
import Std.Diagnostics.*;
import Std.Math.*;
import Std.Measurement.*;

operation Main() : Bool
{
    let markedElements = [];
    mutable result1Count = 0;
    for i in 1..10
    {
        set result1Count += Solve(2, (register, target) => ApplyOracle(2, markedElements, register, target)) ? 1 | 0;
    }
    LogMessage(result1Count == 10, "You have successfully identified a constant oracle function that marked all bits as zero", "You have failed to identify a constant oracle function that marked all bits as zero");

    let markedElements = [0,1,2,3];
    mutable result2Count = 0;
    for i in 1..10
    {
        set result2Count += Solve(2, (register, target) => ApplyOracle(2, markedElements, register, target)) ? 1 | 0;
    }
    LogMessage(result2Count == 10, "You have successfully identified a constant oracle function that marked all bits as one", "You have failed to identify a constant oracle function that marked all bits as one");

    let markedElements = [0, 2];
    mutable result3Count = 0;
    for i in 1..10
    {
        set result3Count += Solve(2, (register, target) => ApplyOracle(2, markedElements, register, target)) ? 0 | 1;
    }
    LogMessage(result3Count == 10, "You have successfully identified a balanced oracle function that marked two out of four bits as one", "You have failed to identify a balanced oracle function that marked two out of four bits as one");

    let markedElements = [1, 3];
    mutable result4Count = 0;
    for i in 1..10
    {
        set result4Count += Solve(2, (register, target) => ApplyOracle(2, markedElements, register, target)) ? 0 | 1;
    }
    LogMessage(result4Count == 10, "You have successfully identified a balanced oracle function that marked two other out of four bits as one", "You have failed to identify a balanced oracle function that marked two other out of four bits as one");

    return result1Count == 10 and result2Count == 10 and result3Count == 10 and result4Count == 10;
}

function LogMessage(isValid: Bool, validMessage: String, invalidMessage: String) : ()
{
    let message = "{\"valid\": " + (isValid ? "true" | "false") + ", \"message\": \"" + (isValid ? validMessage | invalidMessage) + "\"}";
    Message(message);
}

operation ApplyOracle(n : Int, markedElements : Int[], query : Qubit[], target : Qubit) : Unit 
{
    for markedElement in markedElements 
    {
        ApplyControlledOnInt(markedElement, (register) => ApplyToEachCA(X, register), query, [target]);
    }
}

// ===SOLVE-START===
operation Solve (n : Int, oracle : (Qubit[], Qubit) => Unit) : Bool
{
    use (qsInput, qOutput) = (Qubit[n], Qubit());
    
    ApplyToEach(H, qsInput);
    
    X(qOutput);
    H(qOutput);
    
    oracle(qsInput, qOutput);
    
    ApplyToEach(H, qsInput);

    mutable isConstant = true;

    for q in qsInput
    {
        if (M(q) == One) 
        {
            set isConstant = false;
        }
    }

    ResetAll(qsInput);
    Reset(qOutput);  

    return isConstant;
}
// ===SOLVE-END===
