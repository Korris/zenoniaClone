import bcrypt from 'bcryptjs';

/** bcrypt hashing. bcrypt ignores bytes past 72, so callers must validate password length (see auth schemas). */
export function createPasswordService(cost) {
  // A real hash of a random string: compared against when the account does not exist so timing does not reveal it.
  const dummyHash = bcrypt.hashSync('dummy-password-for-timing', cost);
  return {
    hash: (plain) => bcrypt.hash(plain, cost),
    verify: (plain, hash) => bcrypt.compare(plain, hash),
    verifyDummy: (plain) => bcrypt.compare(plain, dummyHash).then(() => false),
  };
}
