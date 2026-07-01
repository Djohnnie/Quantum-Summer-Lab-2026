// ============================================================
// CHALLENGE METADATA
// Edit the fields below directly. SolutionTemplate and
// VerificationTemplate are NOT stored here - they are derived
// automatically from the code below (see the SOLVE markers).
// After editing, run `python _challenges/generate_challenges.py`
// to regenerate QuantumSummerLab.Tools/Challenges.cs.
// ============================================================
// Name = "C2"
// Title = "Distinguish I, CNOTs and SWAP"
// Description = "You are given an operation that implements a two-qubit unitary transformation: either the identity (I ⊗ I gate), the CNOT gate (either with the first qubit as control and the second qubit as target or vice versa) or the SWAP gate.[BR]Your task is to perform necessary operations and measurements to figure out which unitary it was and to return:[BR]0 if it was the I ⊗ I gate,[BR]1 if it was the CNOT12 gate,[BR]2 if it was the CNOT21 gate,[BR]3 if it was the SWAP gate.[BR]You are allowed to apply the given operation exactly twice.[BR]You have to implement an operation which takes a two-qubit operation unitary as an input and returns an integer. The operation unitary will accept an array of qubits as input, but it will fail if the array does not contain exactly two qubits. Your code should have the following signature:"
// Tldr = "You should implement the empty Solve operation below and identify if the provided unitary is the I ⊗ I gate (return 0), the CNOT12 gate (return 1), the CNOT21 gate (return 2), or the SWAP gate (return 3)."
// ExampleDescription = ""
// ExampleCode = ""
// ExpectedOutput = "true"
// ===EXPECTED-STATES-START===
// ===EXPECTED-STATES-END===
// CopilotInstructions = ""
// Level = 3
// ============================================================

import Std.Convert.ResultArrayAsInt;
import Std.Arithmetic.*;
import Std.Canon.*;
import Std.Diagnostics.*;
import Std.Math.*;
import Std.Measurement.*;

operation Main() : Bool
{
    // Wrap the I ⊗ I gate in a unitary operation to be able to check the number of times it was applied.
    let unitary0 = qs => ApplyToEach(I, qs);
    StartCountingOperation(unitary0);
    mutable result0Count = 0;
    for i in 1..10
    {
        let result0 = Solve(unitary0);
        result0Count += result0 == 0 ? 1 | 0;
    }
    LogMessage(result0Count == 10, "You have successfully identified the I ⊗ I gate", "You have failed to identify the I ⊗ I gate");
    let numberOfUnitary0 = StopCountingOperation(unitary0);
    LogMessage(numberOfUnitary0 == 20, "You have successfully applied the unitary operation exactly twice while identifying the I ⊗ I gate", $"You have failed to apply the unitary operation exactly twice and applied it {numberOfUnitary0} times while identifying the I ⊗ I gate");

    // Wrap the CNOT12 gate in a unitary operation to be able to check the number of times it was applied.
    let unitary1 = qs => CNOT(qs[0], qs[1]);
    StartCountingOperation(unitary1);
    mutable result1Count = 0;
    for i in 1..10
    {
        let result1 = Solve(unitary1);
        result1Count += result1 == 1 ? 1 | 0;
    }
    LogMessage(result1Count == 10, "You have successfully identified the CNOT12 gate", "You have failed to identify the CNOT12 gate");
    let numberOfUnitary1 = StopCountingOperation(unitary1);
    LogMessage(numberOfUnitary1 == 20, "You have successfully applied the unitary operation exactly twice while identifying the CNOT12 gate", $"You have failed to apply the unitary operation exactly twice and applied it {numberOfUnitary1} times while identifying the CNOT12 gate");

    // Wrap the CNOT21 gate in a unitary operation to be able to check the number of times it was applied.
    let unitary2 = qs => CNOT(qs[1], qs[0]);
    StartCountingOperation(unitary2);
    mutable result2Count = 0;
    for i in 1..10
    {
        let result2 = Solve(unitary2);
        result2Count += result2 == 2 ? 1 | 0;
    }
    LogMessage(result2Count == 10, "You have successfully identified the CNOT21 gate", "You have failed to identify the CNOT21 gate");
    let numberOfUnitary2 = StopCountingOperation(unitary2);
    LogMessage(numberOfUnitary2 == 20, "You have successfully applied the unitary operation exactly twice while identifying the CNOT21 gate", $"You have failed to apply the unitary operation exactly twice and applied it {numberOfUnitary2} times while identifying the CNOT21 gate");

    // Wrap the SWAP gate in a unitary operation to be able to check the number of times it was applied.
    let unitary3 = qs => SWAP(qs[0], qs[1]);
    StartCountingOperation(unitary3);
    mutable result3Count = 0;
    for i in 1..10
    {
        let result3 = Solve(unitary3);
        result3Count += result3 == 3 ? 1 | 0;
    }
    LogMessage(result3Count == 10, "You have successfully identified the SWAP gate", "You have failed to identify the SWAP gate");
    let numberOfUnitary3 = StopCountingOperation(unitary3);
    LogMessage(numberOfUnitary3 == 20, "You have successfully applied the unitary operation exactly twice while identifying the SWAP gate", $"You have failed to apply the unitary operation exactly twice and applied it {numberOfUnitary3} times while identifying the SWAP gate");

    // Return true if the I ⊗ I gate was identified as 0, the CNOT12 gate as 1, the CNOT21 gate as 2, the SWAP gate as 3, and all unitary operations were applied exactly twice.
    return result0Count == 10 and numberOfUnitary0 == 20 and result1Count == 10 and numberOfUnitary1 == 20 and result2Count == 10 and numberOfUnitary2 == 20 and result3Count == 10 and numberOfUnitary3 == 20;
}

function LogMessage(isValid: Bool, validMessage: String, invalidMessage: String) : ()
{
    let message = "{\"valid\": " + (isValid ? "true" | "false") + ", \"message\": \"" + (isValid ? validMessage | invalidMessage) + "\"}";
    Message(message);
}

// ===SOLVE-START===
operation Solve (unitary : (Qubit[] => Unit)) : Int
{
    // Prepare four qubits in the |0000⟩ state.
    use qs = Qubit[4];

    // Prepare the |0110⟩ state by applying the X gate to the second and third qubit.
    ApplyToEach(X, qs[1..2]);

    // Apply the two-qubit unitary transformation twice.
    // First, apply it to the first two qubits, then to the last two qubits.
    unitary(qs[0..1]);
    unitary(qs[2..3]);

    // Measure the first and last qubits.
    let b1 = MResetZ(qs[0]);
    let b2 = MResetZ(qs[3]);

    // Reset the qubits.
    MResetEachZ(qs);

    // Return an integer based on the measurement results using the little-endian format.
    return ResultArrayAsInt([b2, b1]);
}
// ===SOLVE-END===
