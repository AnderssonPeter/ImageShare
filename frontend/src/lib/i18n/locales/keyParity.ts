import type en from "@lib/i18n/locales/en";
import type sv from "@lib/i18n/locales/sv";

type KeyTree<Type> = Type extends string
  ? null
  : Type extends readonly unknown[]
    ? null
    : { [Key in keyof Type]: KeyTree<Type[Key]> };

type IsEqual<First, Second> =
  (<Type>() => Type extends First ? 1 : 2) extends <Type>() => Type extends Second ? 1 : 2
    ? true
    : false;

type AssertTrue<Type extends true> = Type;

export type KeyParityCheck = AssertTrue<IsEqual<KeyTree<typeof en>, KeyTree<typeof sv>>>;
