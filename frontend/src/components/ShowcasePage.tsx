/**
 * ShowcasePage — development-only gallery of all UI components.
 *
 * Displays every component in the design system (Logo, Button, Input, Label,
 * Tooltip, Carousel, ThemeToggle, MetroAppBar, metro-tile) in a single
 * scrollable page. Gated by `import.meta.env.DEV` in App.tsx via a lazy
 * dynamic import so the showcase code is tree-shaken from production builds.
 */
import { Folder, Star } from 'lucide-react'
import Button from '@/components/ui/Button'
import Carousel from '@/components/ui/Carousel'
import Input from '@/components/ui/Input'
import Label from '@/components/ui/Label'
import Logo from '@/components/Logo'
import MetroAppBar from '@/components/MetroAppBar'
import { type ReactNode } from 'react'
import ThemeToggle from '@/components/ThemeToggle'
import Tooltip from '@/components/ui/Tooltip'

const mockUser = {
  isAuthenticated: true,
  isAdmin: true,
  name: 'Demo User',
}

const carouselOptions = { loop: true }

const tileLabels = [
  'Photos',
  'Videos',
  'Documents',
  'Music',
  'Downloads',
  'Archive',
]

const colorSwatches: readonly { name: string; className: string }[] = [
  { name: 'primary', className: 'bg-primary' },
  { name: 'accent', className: 'bg-accent' },
  { name: 'tile', className: 'bg-tile' },
  { name: 'muted', className: 'bg-muted' },
  { name: 'secondary', className: 'bg-secondary' },
  { name: 'destructive', className: 'bg-destructive' },
  { name: 'background', className: 'bg-background' },
  { name: 'foreground', className: 'bg-foreground' },
]

function ShowcaseSection({
  title,
  children,
}: {
  title: string
  children: ReactNode
}): React.JSX.Element {
  return (
    <section className="flex flex-col gap-gutter py-4">
      <h2 className="text-lg font-semibold text-foreground">{title}</h2>
      {children}
    </section>
  )
}

function LogoSection(): React.JSX.Element {
  return (
    <ShowcaseSection title="Logo">
      <div className="flex items-center gap-gutter">
        <Logo className="size-4 text-primary" />
        <Logo className="size-6 text-primary" />
        <Logo className="size-8 text-primary" />
        <Logo className="size-12 text-primary" />
      </div>
    </ShowcaseSection>
  )
}

function ButtonVariants(): React.JSX.Element {
  return (
    <div className="flex flex-wrap items-center gap-gutter">
      <Button variant="default">Default</Button>
      <Button variant="outline">Outline</Button>
      <Button variant="secondary">Secondary</Button>
      <Button variant="ghost">Ghost</Button>
      <Button variant="destructive">Destructive</Button>
      <Button variant="link">Link</Button>
    </div>
  )
}

function ButtonSizes(): React.JSX.Element {
  return (
    <div className="flex flex-wrap items-center gap-gutter">
      <Button size="xs">XS</Button>
      <Button size="sm">SM</Button>
      <Button>Default</Button>
      <Button size="lg">LG</Button>
      <Button size="icon" aria-label="Star">
        <Star />
      </Button>
    </div>
  )
}

function ButtonSection(): React.JSX.Element {
  return (
    <ShowcaseSection title="Buttons">
      <ButtonVariants />
      <ButtonSizes />
    </ShowcaseSection>
  )
}

function InputSection(): React.JSX.Element {
  return (
    <ShowcaseSection title="Input & Label">
      <div className="flex flex-col gap-gutter">
        <Label htmlFor="showcase-input">Label</Label>
        <Input id="showcase-input" placeholder="Placeholder text" />
      </div>
    </ShowcaseSection>
  )
}

function TooltipSection(): React.JSX.Element {
  return (
    <ShowcaseSection title="Tooltip">
      <Tooltip.Tooltip>
        <Tooltip.TooltipTrigger
          className={Button.buttonVariants({ variant: 'outline' })}
        >
          Hover me
        </Tooltip.TooltipTrigger>
        <Tooltip.TooltipContent>Hello from tooltip!</Tooltip.TooltipContent>
      </Tooltip.Tooltip>
    </ShowcaseSection>
  )
}

function CarouselSlides(): React.JSX.Element {
  return (
    <>
      {tileLabels.map((text) => (
        <Carousel.CarouselItem key={text}>
          <div className="metro-tile h-32 text-lg">
            {text}
          </div>
        </Carousel.CarouselItem>
      ))}
    </>
  )
}

function CarouselBody(): React.JSX.Element {
  return (
    <>
      <Carousel.CarouselContent>
        <CarouselSlides />
      </Carousel.CarouselContent>
      <Carousel.CarouselPrevious />
      <Carousel.CarouselNext />
    </>
  )
}

function CarouselSection(): React.JSX.Element {
  return (
    <ShowcaseSection title="Carousel">
      <Carousel.Carousel opts={carouselOptions}>
        <CarouselBody />
      </Carousel.Carousel>
    </ShowcaseSection>
  )
}

const tileImageStyle = {
  '--metro-tile-image': 'url(/src/assets/showcase-tile.svg)',
} as React.CSSProperties

function TileGrid(): React.JSX.Element {
  return (
    <div className="grid grid-cols-2 gap-gutter sm:grid-cols-3 md:grid-cols-4">
      {tileLabels.map((label) => (
        <div key={label} className="metro-tile h-24">
          <Folder className="metro-tile-icon size-8" />
          <span className="absolute bottom-1 left-1 text-sm leading-none">{label}</span>
        </div>
      ))}
      <div className="metro-tile h-24" style={tileImageStyle}>
        <span className="absolute bottom-1 left-1 text-sm leading-none text-white">With image</span>
      </div>
    </div>
  )
}

function TileSection(): React.JSX.Element {
  return (
    <ShowcaseSection title="Metro Tiles">
      <TileGrid />
    </ShowcaseSection>
  )
}

function ColorSwatch({
  name,
  className,
}: {
  name: string
  className: string
}): React.JSX.Element {
  return (
    <div className="flex flex-col items-center gap-gutter">
      <div className={`size-16 border border-border ${className}`} />
      <span className="text-xs text-muted-foreground">{name}</span>
    </div>
  )
}

function ColorSwatches(): React.JSX.Element {
  return (
    <div className="flex flex-wrap gap-gutter">
      {colorSwatches.map((swatch) => (
        <ColorSwatch
          key={swatch.name}
          name={swatch.name}
          className={swatch.className}
        />
      ))}
    </div>
  )
}

function ColorSection(): React.JSX.Element {
  return (
    <ShowcaseSection title="Color Palette">
      <ColorSwatches />
    </ShowcaseSection>
  )
}

function ThemeSection(): React.JSX.Element {
  return (
    <ShowcaseSection title="Theme Toggle">
      <ThemeToggle />
    </ShowcaseSection>
  )
}

function ShowcaseContent(): React.JSX.Element {
  return (
    <div className="mx-auto flex max-w-4xl flex-col gap-gutter p-4">
      <p className="text-sm text-muted-foreground">
        Development showcase of all UI components.
      </p>
      <LogoSection />
      <ButtonSection />
      <InputSection />
      <TooltipSection />
      <CarouselSection />
      <TileSection />
      <ColorSection />
      <ThemeSection />
    </div>
  )
}

export default function ShowcasePage(): React.JSX.Element {
  return (
    <MetroAppBar user={mockUser} breadcrumb="Showcase">
      <ShowcaseContent />
    </MetroAppBar>
  )
}
