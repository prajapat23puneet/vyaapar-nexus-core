using Xunit;

// Run all test classes sequentially to prevent DB connection pool exhaustion
// and stock-quantity race conditions across saga tests.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
