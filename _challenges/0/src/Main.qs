// ============================================================
// CHALLENGE METADATA
// Edit the fields below directly. SolutionTemplate and
// VerificationTemplate are NOT stored here - they are derived
// automatically from the code below (see the SOLVE markers).
// After editing, run `python _challenges/generate_challenges.py`
// to regenerate QuantumSummerLab.Tools/Challenges.cs.
// ============================================================
// Name = "0"
// Title = "Example Challenge: Prepare |0⟩ or |1⟩"
// Description = "You are given a single qubit, prepared in the |0⟩ state, and a Result that is either Zero or One.[BR]Prepare the qubit in the |0⟩ or |1⟩ state that corresponds to the provided Result value.[BR]You should implement the following Solve operation to make that happen and keep the signature of the operation exactly as it is.[BR]Go ahead and copy/paste the following template in your Q# project in Visual Studio Code to start working on the solution."
// Tldr = "You should implement the empty Solve operation below and prepare a |0⟩ or |1⟩ quantum state depending on the provided expectedResult value."
// ExampleDescription = "Below, you can find a possible solution and additionally, just for reference, the code that will be executed internally to validate the submitted solution.[BR]You should only submit the Solve operation with your implemented solution, or in this example case, a copy from the provided solution below."
// ExampleCode = "b3BlcmF0aW9uIE1haW4oKSA6IEJvb2wKeyAgICAKICAgIHVzZSBxID0gUXViaXQoKTsKCiAgICBTb2x2ZShxLCBaZXJvKTsKICAgIGxldCBiMSA9IE0ocSk7CiAgICBSZXNldChxKTsKCiAgICBTb2x2ZShxLCBPbmUpOwogICAgbGV0IGIyID0gTShxKTsKICAgIFJlc2V0KHEpOwoKICAgIHJldHVybiBiMSA9PSBaZXJvIGFuZCBiMiA9PSBPbmU7Cn0KCm9wZXJhdGlvbiBTb2x2ZSAocSA6IFF1Yml0LCBleHBlY3RlZFJlc3VsdCA6IFJlc3VsdCkgOiBVbml0CnsKICAgIGlmKGV4cGVjdGVkUmVzdWx0ID09IE9uZSkgewogICAgICAgIFgocSk7CiAgICB9Cn0="
// ExpectedOutput = "true"
// ===EXPECTED-STATES-START===
// ===EXPECTED-STATES-END===
// CopilotInstructions = "Challenge 0 is an example challenge that demonstrates how to use this platform to solve challenges. You can help the user in any way you can."
// Level = 0
// ============================================================

import Std.Arithmetic.*;
import Std.Canon.*;
import Std.Diagnostics.*;
import Std.Math.*;
import Std.Measurement.*;

operation Main() : Bool
{ 
    let result1 = CheckOperationsAreEqual(1, x => Solve(x[0], Zero), x => Expected(x[0], Zero));
    mutable result1Count = 0;
    for i in 1..10
    {
        use q = Qubit();
        Solve(q, Zero);
        set result1Count += MResetZ(q) == Zero ? 1 | 0;
    }
    LogMessage(result1 and result1Count == 10, "You have successfully prepared the |0⟩ state", "You have failed to prepare the |0⟩ state");

    let result2 = CheckOperationsAreEqual(1, x => Solve(x[0], One), x => Expected(x[0], One));
    mutable result2Count = 0;
    for i in 1..10
    {
        use q = Qubit();
        Solve(q, One);
        set result2Count += MResetZ(q) == One ? 1 | 0;
    }
    LogMessage(result2 and result2Count == 10, "You have successfully prepared the |1⟩ state", "You have failed to prepare the |1⟩ state");

    return result1 and result1Count == 10 and result2 and result2Count == 10;
}

function LogMessage(isValid: Bool, validMessage: String, invalidMessage: String) : ()
{
    let message = "{\"valid\": " + (isValid ? "true" | "false") + ", \"message\": \"" + (isValid ? validMessage | invalidMessage) + "\"}";
    Message(message);
}

operation Expected (q : Qubit, expectedResult : Result) : Unit is Adj
{
    if(expectedResult == One) {
        X(q);
    }
}

// ===SOLVE-START===
operation Solve (q : Qubit, expectedResult : Result) : Unit
{
    if(expectedResult == One) {
        X(q);
    }
}
// ===SOLVE-END===
