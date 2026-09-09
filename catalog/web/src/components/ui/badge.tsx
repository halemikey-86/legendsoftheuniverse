import type { ComponentProps } from "react";
import { cva, type VariantProps } from "class-variance-authority";
import { cn } from "@/lib/utils";

const badgeVariants = cva(
  "inline-flex items-center rounded-full border px-2.5 py-0.5 text-xs font-medium tracking-wide",
  {
    variants: {
      variant: {
        default: "border-transparent bg-secondary text-foreground",
        outline: "border-border text-muted-foreground",
        gold: "border-gold/40 bg-gold/10 text-gold",
        universe: "border-universe/40 bg-universe/10 text-universe",
        will: "border-will/40 bg-will/10 text-will",
        surge: "border-surge/40 bg-surge/10 text-surge",
        bond: "border-bond/40 bg-bond/10 text-bond",
      },
    },
    defaultVariants: { variant: "default" },
  },
);

function Badge({
  className,
  variant,
  ...props
}: ComponentProps<"span"> & VariantProps<typeof badgeVariants>) {
  return (
    <span className={cn(badgeVariants({ variant }), className)} {...props} />
  );
}

export { Badge, badgeVariants };
